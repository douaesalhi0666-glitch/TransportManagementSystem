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

        // GET: /Clustering/DispatcherView
        public IActionResult DispatcherView()
        {
            return View();
        }

        // API : /Clustering/GetClusters?k=5
        [HttpGet]
        public async Task<IActionResult> GetClusters(int k = 5)
        {
            var personnel = await _context.Personnel
                .Where(p => p.Personnel_Latitude != null && p.Personnel_Longitude != null)
                .Select(p => new { p.Personnel_Id, p.Personnel_FirstName, p.Personnel_LastName, lat = p.Personnel_Latitude.Value, lng = p.Personnel_Longitude.Value })
                .ToListAsync();

            if (personnel.Count == 0)
                return Ok(new { clusters = new List<object>(), points = new List<object>() });

            // Conversion en liste de points (Lat, Lng)
            var points = personnel.Select(p => new ClusterPoint { X = (double)p.lat, Y = (double)p.lng, Data = p }).ToList();

            // Exécution KMeans
            var clusters = KMeans(points, k);

            // Préparer la réponse : centres et points affectés
            var result = new
            {
                clusters = clusters.Select(c => new { lat = c.CenterX, lng = c.CenterY, size = c.Points.Count }),
                points = clusters.SelectMany(c => c.Points.Select(p => new
                {
                    lat = p.X,
                    lng = p.Y,
                    personnelId = ((dynamic)p.Data).Personnel_Id,
                    name = ((dynamic)p.Data).Personnel_FirstName + " " + ((dynamic)p.Data).Personnel_LastName
                }))
            };
            return Ok(result);
        }

        // POST : /Clustering/CreateStop
        [HttpPost]
        public async Task<IActionResult> CreateStop([FromBody] CreateStopModel model)
        {
            if (model == null || model.Latitude == 0 || model.Longitude == 0)
                return BadRequest("Coordonnées invalides");

            var stop = new SuggestedStop
            {
                Name = model.Name,
                Latitude = (decimal)model.Latitude,
                Longitude = (decimal)model.Longitude,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            _context.SuggestedStops.Add(stop);
            await _context.SaveChangesAsync();
            return Ok(new { id = stop.Id });
        }

        // --- Implémentation KMeans ---
        private List<Cluster> KMeans(List<ClusterPoint> points, int k, int maxIterations = 100)
        {
            if (points.Count == 0) return new List<Cluster>();
            Random rand = new Random();
            // Initialisation des centres aléatoires parmi les points
            var centers = points.OrderBy(x => rand.Next()).Take(k).Select(p => new { X = p.X, Y = p.Y }).ToList();

            var clusters = new List<Cluster>();
            for (int iter = 0; iter < maxIterations; iter++)
            {
                // Assignation de chaque point au centre le plus proche
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

                // Recalcul des centres
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

        private double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        // Classes auxiliaires
        private class ClusterPoint
        {
            public double X { get; set; }
            public double Y { get; set; }
            public object Data { get; set; }
        }

        private class Cluster
        {
            public double CenterX { get; set; }
            public double CenterY { get; set; }
            public List<ClusterPoint> Points { get; set; } = new List<ClusterPoint>();
        }
    }

    public class CreateStopModel
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}