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

        // ================================================
        // STOPS VIEWER PAGE
        // ================================================

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
                    s.TS_TrajectoryId,
                    Workers = _context.Personnel
                        .Where(p => p.AssignedStopId == s.TS_Id && p.IsAssigned == true)
                        .Select(p => new
                        {
                            p.Personnel_Id,
                            p.Personnel_FirstName,
                            p.Personnel_LastName,
                            p.Personnel_PhoneNumber,
                            p.Personnel_Email
                        })
                        .ToList()
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
                            s.TS_Longitude,
                            WorkerCount = _context.Personnel
                                .Count(p => p.AssignedStopId == s.TS_Id && p.IsAssigned == true)
                        })
                })
                .ToListAsync();
            return Ok(trajectories);
        }

        [HttpGet]
        public async Task<IActionResult> GetFilteredStops(int? trajectoryId)
        {
            var query = _context.TrajectoryStops
                .Include(s => s.Trajectory)
                .AsQueryable();

            if (trajectoryId.HasValue && trajectoryId.Value > 0)
            {
                query = query.Where(s => s.TS_TrajectoryId == trajectoryId.Value);
            }

            var stops = await query
                .OrderBy(s => s.TS_TrajectoryId)
                .ThenBy(s => s.TS_OrderIndex)
                .Select(s => new
                {
                    s.TS_Id,
                    s.TS_Name,
                    s.TS_OrderIndex,
                    s.TS_Latitude,
                    s.TS_Longitude,
                    s.TS_TrajectoryId,
                    TrajectoryName = s.Trajectory != null ? s.Trajectory.Trajectory_Name : "Inconnue",
                    TrajectoryCode = s.Trajectory != null ? s.Trajectory.Trajectory_Code : "N/A",
                    Workers = _context.Personnel
                        .Where(p => p.AssignedStopId == s.TS_Id && p.IsAssigned == true)
                        .Select(p => new
                        {
                            p.Personnel_Id,
                            p.Personnel_FirstName,
                            p.Personnel_LastName,
                            p.Personnel_PhoneNumber
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(stops);
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkersByStop(int stopId)
        {
            var stop = await _context.TrajectoryStops
                .FirstOrDefaultAsync(s => s.TS_Id == stopId);

            if (stop == null)
                return NotFound(new { message = "Arrêt non trouvé" });

            var workers = await _context.Personnel
                .Where(p => p.AssignedStopId == stopId && p.IsAssigned == true)
                .Select(p => new
                {
                    p.Personnel_Id,
                    p.Personnel_FirstName,
                    p.Personnel_LastName,
                    p.Personnel_PhoneNumber,
                    p.Personnel_Email,
                    p.HomeAddress
                })
                .ToListAsync();

            return Ok(new
            {
                stopId = stop.TS_Id,
                stopName = stop.TS_Name,
                workers = workers
            });
        }

        // ========== WORKER ASSIGNMENT METHODS - FIXED ==========

        [HttpGet]
        public async Task<IActionResult> GetUnassignedWorkersForTrajectory(int trajectoryId)
        {
            // Get workers assigned to this trajectory (either directly or via PersonnelTrajectoryAssignments)
            var workers = await _context.Personnel
                .Where(p => (p.AssignedTrajectoryId == trajectoryId ||
                             _context.PersonnelTrajectoryAssignments.Any(pta => pta.PTA_PersonnelId == p.Personnel_Id && pta.PTA_TrajectoryId == trajectoryId))
                            && p.IsAssigned == true
                            && p.AssignedStopId == null)
                .Select(p => new
                {
                    p.Personnel_Id,
                    p.Personnel_FirstName,
                    p.Personnel_LastName,
                    p.Personnel_PhoneNumber,
                    p.Personnel_Email,
                    p.HomeAddress,
                    p.Personnel_Latitude,
                    p.Personnel_Longitude
                })
                .ToListAsync();

            return Ok(workers);
        }

        [HttpPost]
        public async Task<IActionResult> AssignWorkerToStop([FromBody] AssignWorkerRequest request)
        {
            if (request.WorkerId <= 0 || request.StopId <= 0)
                return BadRequest(new { success = false, message = "Données invalides" });

            var worker = await _context.Personnel.FindAsync((long)request.WorkerId);
            if (worker == null)
                return NotFound(new { success = false, message = "Travailleur non trouvé" });

            var stop = await _context.TrajectoryStops.FindAsync(request.StopId);
            if (stop == null)
                return NotFound(new { success = false, message = "Arrêt non trouvé" });

            worker.AssignedStopId = request.StopId;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Travailleur {worker.Personnel_FirstName} {worker.Personnel_LastName} assigné à l'arrêt {stop.TS_Name}"
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveWorkerFromStop([FromBody] RemoveWorkerRequest request)
        {
            if (request.WorkerId <= 0)
                return BadRequest(new { success = false, message = "Données invalides" });

            var worker = await _context.Personnel.FindAsync((long)request.WorkerId);
            if (worker == null)
                return NotFound(new { success = false, message = "Travailleur non trouvé" });

            worker.AssignedStopId = null;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Travailleur {worker.Personnel_FirstName} {worker.Personnel_LastName} retiré de l'arrêt"
            });
        }

        [HttpPost]
        public async Task<IActionResult> AutoAssignWorkersToStop(int stopId)
        {
            var stop = await _context.TrajectoryStops.FindAsync(stopId);
            if (stop == null)
                return BadRequest(new { success = false, message = "Arrêt non trouvé" });

            var trajectoryId = stop.TS_TrajectoryId;

            // Get workers assigned to this trajectory (either directly or via PersonnelTrajectoryAssignments)
            var unassignedWorkers = await _context.Personnel
                .Where(p => (p.AssignedTrajectoryId == trajectoryId ||
                             _context.PersonnelTrajectoryAssignments.Any(pta => pta.PTA_PersonnelId == p.Personnel_Id && pta.PTA_TrajectoryId == trajectoryId))
                            && p.IsAssigned == true
                            && p.AssignedStopId == null
                            && p.Personnel_Latitude != null
                            && p.Personnel_Longitude != null)
                .ToListAsync();

            if (!unassignedWorkers.Any())
            {
                return Ok(new { success = true, message = "Aucun personnel non assigné trouvé pour cette trajectoire." });
            }

            int assignedCount = 0;
            foreach (var worker in unassignedWorkers)
            {
                double distance = CalculateDistance(
                    (double)stop.TS_Latitude,
                    (double)stop.TS_Longitude,
                    (double)worker.Personnel_Latitude.Value,
                    (double)worker.Personnel_Longitude.Value
                );

                if (distance <= 5.0)
                {
                    worker.AssignedStopId = stopId;
                    assignedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"{assignedCount} travailleurs assignés automatiquement à l'arrêt {stop.TS_Name}"
            });
        }

        [HttpPost]
        public async Task<IActionResult> AutoAssignAllWorkersToNearestStop(int trajectoryId)
        {
            var stops = await _context.TrajectoryStops
                .Where(s => s.TS_TrajectoryId == trajectoryId)
                .OrderBy(s => s.TS_OrderIndex)
                .ToListAsync();

            if (!stops.Any())
                return BadRequest(new { success = false, message = "Aucun arrêt trouvé pour cette trajectoire" });

            // Get workers assigned to this trajectory (either directly or via PersonnelTrajectoryAssignments)
            var unassignedWorkers = await _context.Personnel
                .Where(p => (p.AssignedTrajectoryId == trajectoryId ||
                             _context.PersonnelTrajectoryAssignments.Any(pta => pta.PTA_PersonnelId == p.Personnel_Id && pta.PTA_TrajectoryId == trajectoryId))
                            && p.IsAssigned == true
                            && p.AssignedStopId == null
                            && p.Personnel_Latitude != null
                            && p.Personnel_Longitude != null)
                .ToListAsync();

            if (!unassignedWorkers.Any())
            {
                return Ok(new { success = true, message = "Aucun personnel non assigné trouvé pour cette trajectoire." });
            }

            int assignedCount = 0;
            foreach (var worker in unassignedWorkers)
            {
                TrajectoryStop nearestStop = null;
                double minDistance = double.MaxValue;

                foreach (var stop in stops)
                {
                    double distance = CalculateDistance(
                        (double)stop.TS_Latitude,
                        (double)stop.TS_Longitude,
                        (double)worker.Personnel_Latitude.Value,
                        (double)worker.Personnel_Longitude.Value
                    );

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestStop = stop;
                    }
                }

                if (nearestStop != null)
                {
                    worker.AssignedStopId = nearestStop.TS_Id;
                    assignedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"{assignedCount} travailleurs assignés automatiquement à l'arrêt le plus proche"
            });
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        // ================================================
        // CLUSTERING FOR PICKUP POINTS
        // ================================================

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

            // Récupérer les personnels avec coordonnées
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
            k = Math.Max(1, Math.Min(k, personnelPoints.Count));

            var clusters = KMeansDeterministic(personnelPoints, k);

            // Récupérer les arrêts existants pour cette trajectoire
            var existingStops = await _context.TrajectoryStops
                .Where(s => s.TS_TrajectoryId == request.TrajectoryId)
                .Select(s => new { s.TS_Latitude, s.TS_Longitude })
                .ToListAsync();

            var uniqueClusters = new List<object>();
            var seenKeys = new HashSet<string>();

            foreach (var cluster in clusters)
            {
                if (cluster.Points.Count > 0)
                {
                    decimal centerLat = Math.Round((decimal)cluster.CenterX, 6);
                    decimal centerLng = Math.Round((decimal)cluster.CenterY, 6);

                    string key = $"{centerLat},{centerLng}";

                    if (!seenKeys.Contains(key))
                    {
                        seenKeys.Add(key);

                        // Vérifier si un arrêt existe déjà à moins de 100 mètres de ce centre
                        bool exists = existingStops.Any(stop =>
                        {
                            double dist = CalculateDistanceOptimized(
                                (double)centerLat,
                                (double)centerLng,
                                (double)stop.TS_Latitude,
                                (double)stop.TS_Longitude);
                            return dist < 0.1; // 100 mètres
                        });

                        uniqueClusters.Add(new
                        {
                            lat = centerLat,
                            lng = centerLng,
                            count = cluster.Points.Count,
                            exists = exists
                        });
                    }
                }
            }

            var result = new
            {
                clusters = uniqueClusters,
                points = personnelPoints.Select(p => new
                {
                    lat = Math.Round((decimal)p.X, 6),
                    lng = Math.Round((decimal)p.Y, 6)
                })
            };
            return Ok(result);
        }

        private List<Cluster> KMeansDeterministic(List<ClusterPoint> points, int k, int maxIterations = 100)
        {
            if (points.Count == 0) return new List<Cluster>();
            if (k > points.Count) k = points.Count;

            Random rand = new Random(42);

            var distinctPoints = points
                .Select(p => new { p.X, p.Y })
                .Distinct()
                .ToList();

            var centers = distinctPoints
                .OrderBy(x => rand.Next())
                .Take(k)
                .Select(p => new { X = p.X, Y = p.Y })
                .ToList();

            var clusters = new List<Cluster>();

            for (int iter = 0; iter < maxIterations; iter++)
            {
                clusters = new List<Cluster>();
                for (int i = 0; i < centers.Count; i++)
                    clusters.Add(new Cluster { CenterX = centers[i].X, CenterY = centers[i].Y });

                foreach (var point in points)
                {
                    double minDist = double.MaxValue;
                    int bestCluster = 0;
                    for (int i = 0; i < centers.Count; i++)
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
                for (int i = 0; i < centers.Count; i++)
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

            return clusters.Where(c => c.Points.Count > 0).ToList();
        }

        [HttpPost]
        public async Task<IActionResult> CreateStopForTrajectory([FromBody] CreateTrajectoryStopModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new { success = false, message = "Nom invalide" });
            }

            // Accepter trajectoryId = 0 (point sans trajectoire)
            int maxOrder = await _context.TrajectoryStops
                .Where(s => s.TS_TrajectoryId == model.TrajectoryId)
                .MaxAsync(s => (int?)s.TS_OrderIndex) ?? 0;

            var stop = new TrajectoryStop
            {
                TS_TrajectoryId = model.TrajectoryId, // Peut être 0
                TS_Name = model.Name.Trim(),
                TS_OrderIndex = maxOrder + 1,
                TS_Latitude = model.Latitude,
                TS_Longitude = model.Longitude
            };

            try
            {
                _context.TrajectoryStops.Add(stop);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, stopId = stop.TS_Id, message = "Point créé avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        public IActionResult DefinePickupPoints()
        {
            return View();
        }

        // ================================================
        // DELETE AND UPDATE STOP METHODS
        // ================================================

        [HttpDelete]
        [HttpDelete]
        public async Task<IActionResult> DeleteStop(int id)
        {
            var stop = await _context.TrajectoryStops.FindAsync(id);
            if (stop == null)
                return NotFound(new { success = false, message = "Arrêt non trouvé" });

            int trajectoryId = stop.TS_TrajectoryId;

            // 1. Supprimer les références dans FragmentStop (liaison avec les fragments)
            var fragmentStops = await _context.FragmentStops
                .Where(fs => fs.TS_Id == id)
                .ToListAsync();
            if (fragmentStops.Any())
            {
                _context.FragmentStops.RemoveRange(fragmentStops);
            }

            // 2. Désassigner les personnels assignés à cet arrêt
            var workersAtStop = await _context.Personnel
                .Where(p => p.AssignedStopId == id)
                .ToListAsync();
            foreach (var worker in workersAtStop)
            {
                worker.AssignedStopId = null;
                worker.IsAssigned = false; // Optionnel : remettre à false
            }

            // 3. Supprimer l'arrêt
            _context.TrajectoryStops.Remove(stop);
            await _context.SaveChangesAsync();

            // 4. Réordonner les arrêts restants de la même trajectoire
            var remainingStops = await _context.TrajectoryStops
                .Where(s => s.TS_TrajectoryId == trajectoryId)
                .OrderBy(s => s.TS_OrderIndex)
                .ToListAsync();
            for (int i = 0; i < remainingStops.Count; i++)
            {
                remainingStops[i].TS_OrderIndex = i + 1;
            }
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Arrêt supprimé avec succès" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStop([FromBody] UpdateStopModel model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { success = false, message = "Données invalides" });

            var stop = await _context.TrajectoryStops.FindAsync(model.Id);
            if (stop == null)
                return NotFound(new { success = false, message = "Arrêt non trouvé" });

            stop.TS_Name = model.Name;
            stop.TS_Latitude = model.Latitude;
            stop.TS_Longitude = model.Longitude;
            stop.TS_OrderIndex = model.OrderIndex;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Arrêt modifié avec succès" });
        }

        // ================================================
        // AI POWERED ROUTE OPTIMIZATION
        // ================================================

        [HttpPost]
        public async Task<IActionResult> GetOptimizedRoute([FromBody] OptimizedRouteRequest request)
        {
            if (request.TrajectoryId <= 0)
                return BadRequest("Trajectoire invalide");

            var stops = await _context.TrajectoryStops
                .Where(s => s.TS_TrajectoryId == request.TrajectoryId)
                .OrderBy(s => s.TS_OrderIndex)
                .ToListAsync();

            if (!stops.Any())
                return Ok(new List<object>());

            var stopsList = stops.Select(s => new
            {
                s.TS_Id,
                s.TS_Name,
                s.TS_OrderIndex,
                Lat = (double)s.TS_Latitude,
                Lng = (double)s.TS_Longitude
            }).ToList();

            var remaining = stopsList.ToList();
            var ordered = new List<object>();
            double currentLat = request.DriverLatitude;
            double currentLng = request.DriverLongitude;

            while (remaining.Any())
            {
                int nearestIdx = 0;
                double nearestDist = CalculateDistanceOptimized(currentLat, currentLng, remaining[0].Lat, remaining[0].Lng);

                for (int i = 1; i < remaining.Count; i++)
                {
                    double dist = CalculateDistanceOptimized(currentLat, currentLng, remaining[i].Lat, remaining[i].Lng);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestIdx = i;
                    }
                }

                var nextStop = remaining[nearestIdx];
                ordered.Add(new
                {
                    nextStop.TS_Id,
                    nextStop.TS_Name,
                    nextStop.TS_OrderIndex,
                    Lat = nextStop.Lat,
                    Lng = nextStop.Lng,
                    OptimizedOrder = ordered.Count + 1,
                    DistanceFromPrevious = Math.Round(nearestDist, 2)
                });

                currentLat = nextStop.Lat;
                currentLng = nextStop.Lng;
                remaining.RemoveAt(nearestIdx);
            }

            return Ok(ordered);
        }

        private double CalculateDistanceOptimized(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        // ================================================
        // UTILITIES
        // ================================================

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

    // ================================================
    // REQUEST MODELS
    // ================================================

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

    public class OptimizedRouteRequest
    {
        public int TrajectoryId { get; set; }
        public double DriverLatitude { get; set; }
        public double DriverLongitude { get; set; }
    }

    public class UpdateStopModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int OrderIndex { get; set; }
    }

    public class AssignWorkerRequest
    {
        public int WorkerId { get; set; }
        public int StopId { get; set; }
    }

    public class RemoveWorkerRequest
    {
        public int WorkerId { get; set; }
    }
}