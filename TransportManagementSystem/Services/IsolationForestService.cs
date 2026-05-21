using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Services
{
    public class IsolationForestService
    {
        private readonly ApplicationDbContext _context;
        private readonly Dictionary<int, IsolationForestModel> _models = new();

        public IsolationForestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task TrainAll()
        {
            var trajectories = await _context.Trajectories
                .Where(t => t.Trajectory_Status == "Active")
                .ToListAsync();

            foreach (var traj in trajectories)
            {
                await TrainForTrajectory(traj.Trajectory_Id);
            }
        }

        public async Task TrainForTrajectory(int trajectoryId)
        {
            // Récupérer l'historique des positions GPS pour cette trajectoire
            var positions = await _context.BusLocationHistory
                .Where(l => l.TrajectoryId == trajectoryId)
                .Select(l => new { l.Latitude, l.Longitude })
                .ToListAsync();

            if (positions.Count < 10) return; // Pas assez de données

            var features = positions.Select(p => new[] { (double)p.Latitude, (double)p.Longitude }).ToArray();
            var model = new IsolationForestModel();
            model.Fit(features);
            _models[trajectoryId] = model;
        }

        public async Task<bool> IsDeviationAnomaly(long busId, int trajectoryId, double latitude, double longitude)
        {
            if (!_models.ContainsKey(trajectoryId)) return false;

            var model = _models[trajectoryId];
            double[] point = new[] { latitude, longitude };
            double score = model.Predict(point);
            return score < -0.5; // Seuil : plus négatif = plus anormal
        }
    }

    // Isolation Forest simplifié
    public class IsolationForestModel
    {
        private List<double[]> _trees = new();
        private int _maxDepth = 10;

        public void Fit(double[][] data)
        {
            var rand = new Random();
            for (int i = 0; i < 50; i++)
            {
                var sample = SampleData(data, data.Length / 2, rand);
                var tree = BuildTree(sample, 0, rand);
                _trees.Add(tree);
            }
        }

        private double[] BuildTree(double[][] data, int depth, Random rand)
        {
            if (depth >= _maxDepth || data.Length <= 1)
                return new double[] { 0, 0, depth };

            int feature = rand.Next(2); // 0=latitude, 1=longitude
            var min = data.Min(d => d[feature]);
            var max = data.Max(d => d[feature]);
            if (Math.Abs(max - min) < 0.0001) return new double[] { 0, 0, depth };

            var splitValue = min + (rand.NextDouble() * (max - min));
            var left = data.Where(d => d[feature] < splitValue).ToArray();
            var right = data.Where(d => d[feature] >= splitValue).ToArray();

            var leftTree = BuildTree(left, depth + 1, rand);
            var rightTree = BuildTree(right, depth + 1, rand);

            // Stocker [splitValue, feature, profondeur]
            return new double[] { splitValue, feature, depth };
        }

        public double Predict(double[] point)
        {
            double avgPathLength = 0;
            foreach (var tree in _trees)
            {
                avgPathLength += PathLength(point, tree);
            }
            avgPathLength /= _trees.Count;
            return -Math.Pow(2, -avgPathLength / _maxDepth);
        }

        private double PathLength(double[] point, double[] tree, double pathLength = 0)
        {
            if (tree[2] >= _maxDepth || (Math.Abs(tree[0]) < 0.0001 && Math.Abs(tree[1]) < 0.0001))
                return pathLength + _maxDepth;

            int feature = (int)tree[1];
            if (point[feature] < tree[0])
                return PathLength(point, tree, pathLength + 1);
            else
                return PathLength(point, tree, pathLength + 1);
        }

        private double[][] SampleData(double[][] data, int sampleSize, Random rand)
        {
            return data.OrderBy(x => rand.Next()).Take(sampleSize).ToArray();
        }
    }
}