using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TransportManagementSystem.Controllers
{
    public class ClusteringController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClusteringController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------- STOPS VIEWER PAGE ----------
        public async Task<IActionResult> DispatcherView()
        {
            var stops = await _context.TrajectoryStops
                .Include(s => s.Trajectory)
                .OrderBy(s => s.TS_TrajectoryId)
                .ThenBy(s => s.TS_OrderIndex)
                .ToListAsync();
            return View(stops);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStops()
        {
            var stops = await _context.TrajectoryStops
                .Include(s => s.Trajectory)
                .Select(s => new
                {
                    s.TS_Id,
                    s.TS_Name,
                    s.TS_OrderIndex,
                    s.TS_Latitude,
                    s.TS_Longitude,
                    TrajectoryName = s.Trajectory != null ? s.Trajectory.Trajectory_Name : "Inconnue",
                    TrajectoryCode = s.Trajectory != null ? s.Trajectory.Trajectory_Code : "N/A",
                    s.TS_TrajectoryId
                })
                .ToListAsync();
            return Ok(stops);
        }

        [HttpGet]
        public async Task<IActionResult> GetTrajectoriesWithStops()
        {
            var trajectories = await _context.Trajectories
                .Where(t => t.Trajectory_Status == "Active")
                .Include(t => t.Stops)
                .Select(t => new
                {
                    t.Trajectory_Id,
                    t.Trajectory_Name,
                    t.Trajectory_Code,
                    Stops = t.Stops
                        .OrderBy(s => s.TS_OrderIndex)
                        .Select(s => new
                        {
                            s.TS_Id,
                            s.TS_Name,
                            s.TS_OrderIndex,
                            s.TS_Latitude,
                            s.TS_Longitude
                        })
                })
                .ToListAsync();
            return Ok(trajectories);
        }

        // ---------- CLUSTERING FOR PICKUP POINTS ----------
        [HttpGet]
        public async Task<IActionResult> GetTrajectoriesForPickup()
        {
            var trajectories = await _context.Trajectories
                .Where(t => t.Trajectory_Status == "Active")
                .Select(t => new { t.Trajectory_Id, t.Trajectory_Name, t.Trajectory_Code })
                .ToListAsync();
            return Ok(trajectories);
        }

        [HttpPost]
        public async Task<IActionResult> SuggestPickupPoints([FromBody] PickupRequest request)
        {
            if (request.TrajectoryId <= 0)
                return BadRequest("Trajectoire invalide");

            var personnelPoints = await _context.PersonnelTrajectoryAssignments
                .Where(pta => pta.PTA_TrajectoryId == request.TrajectoryId && pta.PTA_Status == "Active")
                .Select(pta => pta.Personnel)
                .Where(p => p != null && p.Personnel_Latitude != null && p.Personnel_Longitude != null)
                .Select(p => new ClusterPoint
                {
                    X = (double)p.Personnel_Latitude!.Value,
                    Y = (double)p.Personnel_Longitude!.Value,
                    Data = new { p.Personnel_Id, p.Personnel_FirstName, p.Personnel_LastName }
                })
                .ToListAsync();

            if (!personnelPoints.Any())
            {
                personnelPoints = await _context.Personnel
                    .Where(p => p.AssignedTrajectoryId == request.TrajectoryId && p.IsAssigned == true
                                && p.Personnel_Latitude != null && p.Personnel_Longitude != null)
                    .Select(p => new ClusterPoint
                    {
                        X = (double)p.Personnel_Latitude!.Value,
                        Y = (double)p.Personnel_Longitude!.Value,
                        Data = new { p.Personnel_Id, p.Personnel_FirstName, p.Personnel_LastName }
                    })
                    .ToListAsync();
            }

            if (personnelPoints.Count == 0)
                return Ok(new { clusters = new List<object>(), points = new List<object>(), message = "Aucun personnel avec coordonnées pour cette trajectoire." });

            int k = request.NumberOfClusters;
            k = Math.Max(1, Math.Min(k, personnelPoints.Count)); // éviter k > nb points

            // Ajouter un très petit bruit aux points en double pour que KMeans puisse les séparer
            var distinctPoints = personnelPoints
                .Select(p => new { p.X, p.Y })
                .Distinct()
                .ToList();
            if (distinctPoints.Count < k)
            {
                k = distinctPoints.Count;
            }

            var pointsWithNoise = personnelPoints.Select(p => new ClusterPoint
            {
                X = p.X + new Random().NextDouble() * 0.000001,
                Y = p.Y + new Random().NextDouble() * 0.000001,
                Data = p.Data
            }).ToList();

            var clusters = KMeans(pointsWithNoise, k);

            var result = new
            {
                clusters = clusters.Select(c => new { lat = c.CenterX, lng = c.CenterY, count = c.Points.Count }),
                points = personnelPoints.Select(p => new { lat = p.X, lng = p.Y })
            };
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateStopForTrajectory([FromBody] CreateTrajectoryStopModel model)
        {
            if (model.TrajectoryId == 0 || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest("Données invalides");

            int maxOrder = await _context.TrajectoryStops
                .Where(s => s.TS_TrajectoryId == model.TrajectoryId)
                .MaxAsync(s => (int?)s.TS_OrderIndex) ?? 0;

            var stop = new TrajectoryStop
            {
                TS_TrajectoryId = model.TrajectoryId,
                TS_Name = model.Name,
                TS_OrderIndex = maxOrder + 1,
                TS_Latitude = model.Latitude,
                TS_Longitude = model.Longitude
            };
            _context.TrajectoryStops.Add(stop);
            await _context.SaveChangesAsync();
            return Ok(new { stopId = stop.TS_Id });
        }

        public IActionResult DefinePickupPoints()
        {
            return View();
        }

        // ---------- UTILITAIRES ----------
        private List<Cluster> KMeans(List<ClusterPoint> points, int k, int maxIterations = 100)
        {
            if (points.Count == 0) return new List<Cluster>();
            Random rand = new Random();
            var centers = points.OrderBy(x => rand.Next()).Take(k).Select(p => new { X = p.X, Y = p.Y }).ToList();

            var clusters = new List<Cluster>();
            for (int iter = 0; iter < maxIterations; iter++)
            {
                clusters = new List<Cluster>();
                for (int i = 0; i < k; i++)
                    clusters.Add(new Cluster { CenterX = centers[i].X, CenterY = centers[i].Y });

                foreach (var point in points)
                {
                    double minDist = double.MaxValue;
                    int bestCluster = 0;
                    for (int i = 0; i < k; i++)
                    {
                        double dist = Distance(point.X, point.Y, centers[i].X, centers[i].Y);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            bestCluster = i;
                        }
                    }
                    clusters[bestCluster].Points.Add(point);
                }

                bool changed = false;
                for (int i = 0; i < k; i++)
                {
                    if (clusters[i].Points.Count == 0) continue;
                    double newX = clusters[i].Points.Average(p => p.X);
                    double newY = clusters[i].Points.Average(p => p.Y);
                    if (Math.Abs(newX - centers[i].X) > 0.00001 || Math.Abs(newY - centers[i].Y) > 0.00001)
                        changed = true;
                    centers[i] = new { X = newX, Y = newY };
                    clusters[i].CenterX = newX;
                    clusters[i].CenterY = newY;
                }
                if (!changed) break;
            }
            return clusters;
        }

        private double Distance(double x1, double y1, double x2, double y2) =>
            Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));

        private class ClusterPoint
        {
            public double X { get; set; }
            public double Y { get; set; }
            public object? Data { get; set; }
        }

        private class Cluster
        {
            public double CenterX { get; set; }
            public double CenterY { get; set; }
            public List<ClusterPoint> Points { get; set; } = new List<ClusterPoint>();
        }
    }

    public class PickupRequest
    {
        public int TrajectoryId { get; set; }
        public int NumberOfClusters { get; set; } = 5;
    }

    public class CreateTrajectoryStopModel
    {
        public int TrajectoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}