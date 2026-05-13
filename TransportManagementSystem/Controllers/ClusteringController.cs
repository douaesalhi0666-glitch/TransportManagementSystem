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
        // STOPS VIEWER PAGE (DispatcherView)
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

        public IActionResult DefinePickupPoints()
        {
            return View();
        }

        // ================================================
        // API : Récupérer tous les arrêts (avec personnels)
        // ================================================
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
        public async Task<IActionResult> GetFilteredStops(int? trajectoryId)
        {
            IQueryable<TrajectoryStop> query = _context.TrajectoryStops
                .Include(s => s.Trajectory);

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
                            p.Personnel_LastName
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(stops);
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkersByStop(int stopId)
        {
            var stop = await _context.TrajectoryStops.FindAsync(stopId);
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
                workers
            });
        }

        // ========== WORKER ASSIGNMENT METHODS ==========
        [HttpGet]
        public async Task<IActionResult> GetUnassignedWorkers()
        {
            var workers = await _context.Personnel
                .Where(p => p.IsAssigned == false
                            && p.AssignedStopId == null
                            && p.Personnel_Status == "Active"
                            && p.Personnel_Latitude != null
                            && p.Personnel_Longitude != null)
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
            worker.IsAssigned = true;
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
            worker.IsAssigned = false;
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

            var unassignedWorkers = await _context.Personnel
                .Where(p => p.IsAssigned == false && p.AssignedStopId == null
                            && p.Personnel_Status == "Active"
                            && p.Personnel_Latitude != null && p.Personnel_Longitude != null)
                .ToListAsync();

            if (!unassignedWorkers.Any())
                return Ok(new { success = true, message = "Aucun personnel non assigné trouvé." });

            int assignedCount = 0;
            foreach (var worker in unassignedWorkers)
            {
                double distance = CalculateDistance(
                    (double)stop.TS_Latitude,
                    (double)stop.TS_Longitude,
                    (double)worker.Personnel_Latitude!.Value,
                    (double)worker.Personnel_Longitude!.Value);
                if (distance <= 5.0) // 5 km
                {
                    worker.AssignedStopId = stopId;
                    worker.IsAssigned = true;
                    assignedCount++;
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new
            {
                success = true,
                message = $"{assignedCount} personnel(s) assigné(s) à l'arrêt {stop.TS_Name}"
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
                return BadRequest(new { success = false, message = "Aucun arrêt trouvé" });

            var unassignedWorkers = await _context.Personnel
                .Where(p => p.IsAssigned == false && p.AssignedStopId == null
                            && p.Personnel_Status == "Active"
                            && p.Personnel_Latitude != null && p.Personnel_Longitude != null)
                .ToListAsync();

            if (!unassignedWorkers.Any())
                return Ok(new { success = true, message = "Aucun personnel non assigné" });

            int assignedCount = 0;
            foreach (var worker in unassignedWorkers)
            {
                TrajectoryStop nearestStop = null;
                double minDistance = double.MaxValue;
                double wLat = (double)worker.Personnel_Latitude!.Value;
                double wLng = (double)worker.Personnel_Longitude!.Value;

                foreach (var stop in stops)
                {
                    double dist = CalculateDistance((double)stop.TS_Latitude, (double)stop.TS_Longitude, wLat, wLng);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestStop = stop;
                    }
                }

                if (nearestStop != null)
                {
                    worker.AssignedStopId = nearestStop.TS_Id;
                    worker.IsAssigned = true;
                    assignedCount++;
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = $"{assignedCount} personnel(s) assigné(s)" });
        }

        // ========== CRUD DES POINTS DE RAMASSAGE ==========
        [HttpPost]
        public async Task<IActionResult> CreateStopForTrajectory([FromBody] CreateTrajectoryStopModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { success = false, message = "Nom invalide" });

            try
            {
                // Vérifier que la trajectoire existe
                var trajectory = await _context.Trajectories.FindAsync(model.TrajectoryId);
                if (trajectory == null)
                    return BadRequest(new { success = false, message = $"Trajectoire ID {model.TrajectoryId} n'existe pas." });

                int maxOrder = await _context.TrajectoryStops
                    .Where(s => s.TS_TrajectoryId == model.TrajectoryId)
                    .MaxAsync(s => (int?)s.TS_OrderIndex) ?? 0;

                var stop = new TrajectoryStop
                {
                    TS_TrajectoryId = model.TrajectoryId,
                    TS_Name = model.Name.Trim(),
                    TS_OrderIndex = maxOrder + 1,
                    TS_Latitude = model.Latitude,
                    TS_Longitude = model.Longitude
                };
                _context.TrajectoryStops.Add(stop);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, stopId = stop.TS_Id, message = "Point créé" });
            }
            catch (DbUpdateException ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { success = false, message = inner });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteStop(int id)
        {
            var stop = await _context.TrajectoryStops.FindAsync(id);
            if (stop == null)
                return NotFound(new { success = false, message = "Arrêt non trouvé" });

            var workers = await _context.Personnel.Where(p => p.AssignedStopId == id).ToListAsync();
            foreach (var w in workers)
            {
                w.AssignedStopId = null;
                w.IsAssigned = false;
            }
            _context.TrajectoryStops.Remove(stop);
            await _context.SaveChangesAsync();

            // Réordonner les arrêts de la même trajectoire
            var remaining = await _context.TrajectoryStops
                .Where(s => s.TS_TrajectoryId == stop.TS_TrajectoryId)
                .OrderBy(s => s.TS_OrderIndex)
                .ToListAsync();
            for (int i = 0; i < remaining.Count; i++)
                remaining[i].TS_OrderIndex = i + 1;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Arrêt supprimé" });
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

            return Ok(new { success = true, message = "Arrêt modifié" });
        }

        // ================================================
        // SUGGESTION DE POINTS PAR IA (KMEANS)
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
            var personnelPoints = await _context.Personnel
                .Where(p => p.Personnel_Status == "Active"
                            && p.Personnel_Latitude != null
                            && p.Personnel_Longitude != null)
                .Select(p => new ClusterPoint
                {
                    X = (double)p.Personnel_Latitude!.Value,
                    Y = (double)p.Personnel_Longitude!.Value,
                    Data = new { p.Personnel_Id, p.Personnel_FirstName, p.Personnel_LastName }
                })
                .ToListAsync();

            if (personnelPoints.Count == 0)
                return Ok(new { clusters = new List<object>(), points = new List<object>(), message = "Aucun personnel avec coordonnées." });

            int k = request.NumberOfClusters;
            k = Math.Max(1, Math.Min(k, personnelPoints.Count));

            var clusters = KMeansDeterministic(personnelPoints, k);

            // Récupérer les arrêts existants pour éviter les doublons
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
                        bool exists = existingStops.Any(stop =>
                        {
                            double dist = HaversineDistance(
                                (double)centerLat,
                                (double)centerLng,
                                (double)stop.TS_Latitude,
                                (double)stop.TS_Longitude);
                            return dist < 0.1; // moins de 100 mètres
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

        // ================================================
        // ALGORITHMES KMEANS ET UTILITAIRES
        // ================================================
        private List<Cluster> KMeansDeterministic(List<ClusterPoint> points, int k, int maxIterations = 100)
        {
            if (points.Count == 0) return new List<Cluster>();
            if (k > points.Count) k = points.Count;

            Random rand = new Random(42);
            var distinctPoints = points.Select(p => new { p.X, p.Y }).Distinct().ToList();
            var centers = distinctPoints.OrderBy(x => rand.Next()).Take(k)
                .Select(p => new { X = p.X, Y = p.Y }).ToList();
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
                        if (dist < minDist) { minDist = dist; bestCluster = i; }
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

        private double Distance(double x1, double y1, double x2, double y2) =>
            Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));

        // ================================================
        // DÉCOUPAGE DES ARRÊTS SURCHARGÉS
        // ================================================
        private List<(TrajectoryStop Stop, int Workers)> SplitOverloadedStops(List<(TrajectoryStop Stop, int Workers)> stops, int maxCapacity)
        {
            var result = new List<(TrajectoryStop Stop, int Workers)>();
            int splitCounter = 0;
            foreach (var (stop, workers) in stops)
            {
                if (workers <= maxCapacity)
                {
                    result.Add((stop, workers));
                }
                else
                {
                    int remaining = workers;
                    int busNumber = 1;
                    while (remaining > 0)
                    {
                        int take = Math.Min(remaining, maxCapacity);
                        var virtualStop = new TrajectoryStop
                        {
                            TS_Id = -stop.TS_Id - splitCounter, // ID négatif temporaire
                            TS_Name = $"{stop.TS_Name} (Bus {busNumber})",
                            TS_Latitude = stop.TS_Latitude,
                            TS_Longitude = stop.TS_Longitude,
                            TS_TrajectoryId = stop.TS_TrajectoryId,
                            TS_OrderIndex = stop.TS_OrderIndex
                        };
                        result.Add((virtualStop, take));
                        remaining -= take;
                        busNumber++;
                        splitCounter++;
                    }
                }
            }
            return result;
        }

        // ================================================
        // GÉNÉRATION DE TRAJETS (ALGORITHME GLOUTON AVEC DÉCOUPAGE)
        // ================================================
        [HttpPost]
        public async Task<IActionResult> GenerateSmartTrajectories(int maxTimeMinutes = 60, int busCapacity = 20, double speedKmh = 30)
        {
            try
            {
                var stopsData = await _context.TrajectoryStops
                    .Where(s => s.TS_Latitude != 0 && s.TS_Longitude != 0)
                    .Select(s => new
                    {
                        Stop = s,
                        Workers = _context.Personnel.Count(p => p.AssignedStopId == s.TS_Id && p.IsAssigned == true)
                    })
                    .ToListAsync();

                var validStops = stopsData.Where(s => s.Workers > 0).ToList();
                if (!validStops.Any())
                    return BadRequest("Aucun point de ramassage avec personnels assignés.");

                // Étape 1 : découper les arrêts en sous-arrêts virtuels
                var stopsWithWorkers = validStops.Select(s => (Stop: s.Stop, Workers: s.Workers)).ToList();
                var expandedStops = SplitOverloadedStops(stopsWithWorkers, busCapacity);

                const double startLat = 34.2900, startLng = -6.5700;
                var items = expandedStops.Select(s => new
                {
                    Stop = s.Stop,
                    Workers = s.Workers,
                    TravelTime = HaversineDistance(startLat, startLng, (double)s.Stop.TS_Latitude, (double)s.Stop.TS_Longitude) / 1000.0 / speedKmh * 60.0
                }).OrderBy(x => x.TravelTime).ToList();

                // Étape 2 : regroupement glouton
                var clusters = new List<List<TrajectoryStop>>();
                bool[] used = new bool[items.Count];

                for (int i = 0; i < items.Count; i++)
                {
                    if (used[i]) continue;
                    var cluster = new List<TrajectoryStop>();
                    int currentWorkers = 0;
                    double currentMaxTime = 0;

                    for (int j = 0; j < items.Count; j++)
                    {
                        if (used[j]) continue;
                        int newWorkers = currentWorkers + items[j].Workers;
                        double newMaxTime = Math.Max(currentMaxTime, items[j].TravelTime);
                        if (newWorkers <= busCapacity && newMaxTime <= maxTimeMinutes)
                        {
                            cluster.Add(items[j].Stop);
                            currentWorkers = newWorkers;
                            currentMaxTime = newMaxTime;
                            used[j] = true;
                        }
                    }
                    if (cluster.Any())
                        clusters.Add(cluster);
                }

                var createdTrajs = new List<Trajectory>();
                int counter = 1;

                foreach (var cluster in clusters)
                {
                    var orderedStops = cluster
                        .OrderBy(s => HaversineDistance(startLat, startLng, (double)s.TS_Latitude, (double)s.TS_Longitude))
                        .ToList();

                    if (!orderedStops.Any()) continue;

                    double maxDist = orderedStops.Max(s => HaversineDistance(startLat, startLng, (double)s.TS_Latitude, (double)s.TS_Longitude) / 1000.0);
                    double maxTime = orderedStops.Max(s => HaversineDistance(startLat, startLng, (double)s.TS_Latitude, (double)s.TS_Longitude) / 1000.0 / speedKmh * 60.0);
                    int totalWorkers = cluster.Sum(s => validStops.FirstOrDefault(v => v.Stop.TS_Id == s.TS_Id)?.Workers ?? 0);

                    var traj = new Trajectory
                    {
                        Trajectory_Name = $"Trajet IA-{DateTime.Now:yyyyMMddHHmmss}-{counter}",
                        Trajectory_Code = $"IA-{counter}",
                        Trajectory_Description = $"{orderedStops.Count} arrêts, {totalWorkers} pers",
                        Trajectory_StartLatitude = (decimal)startLat,
                        Trajectory_StartLongitude = (decimal)startLng,
                        Trajectory_EndLatitude = (decimal)orderedStops.Last().TS_Latitude,
                        Trajectory_EndLongitude = (decimal)orderedStops.Last().TS_Longitude,
                        Trajectory_DistanceKm = (decimal)Math.Round(maxDist, 2),
                        Trajectory_EstimatedDurationMinutes = (int)Math.Ceiling(maxTime),
                        Trajectory_Status = "Active",
                        Trajectory_CreatedAt = DateTime.Now,
                        Trajectory_UpdatedAt = DateTime.Now
                    };
                    _context.Trajectories.Add(traj);
                    await _context.SaveChangesAsync();

                    int order = 1;
                    foreach (var s in orderedStops)
                    {
                        s.TS_TrajectoryId = traj.Trajectory_Id;
                        s.TS_OrderIndex = order++;
                        _context.TrajectoryStops.Update(s);
                    }
                    await _context.SaveChangesAsync();
                    createdTrajs.Add(traj);
                    counter++;
                }

                return Ok(new { success = true, count = createdTrajs.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.ToString() });
            }
        }

        // ================================================
        // GÉNÉRATION DE TRAJETS (AFFICHAGE MODAL) - BOUTON "Générer les trajets"
        // ================================================
        [HttpPost]
        public async Task<IActionResult> GenerateRoutesFromPickupPoints([FromBody] GenerateRoutesRequest request)
        {
            try
            {
                int busCapacity = request?.BusCapacity ?? 20;
                double speedKmh = 30;

                var stopsData = await _context.TrajectoryStops
                    .Where(s => s.TS_Latitude != 0 && s.TS_Longitude != 0)
                    .Select(s => new
                    {
                        Stop = s,
                        Workers = _context.Personnel.Count(p => p.AssignedStopId == s.TS_Id && p.IsAssigned == true)
                    })
                    .ToListAsync();

                var validStops = stopsData.Where(s => s.Workers > 0).ToList();
                if (!validStops.Any())
                    return BadRequest(new { success = false, message = "Aucun point de ramassage avec personnels assignés." });

                const double startLat = 34.2900, startLng = -6.5700;
                var items = validStops.Select(s => new
                {
                    Stop = s.Stop,
                    Workers = s.Workers,
                    TravelTime = HaversineDistance(startLat, startLng, (double)s.Stop.TS_Latitude, (double)s.Stop.TS_Longitude) / 1000.0 / speedKmh * 60.0,
                    Distance = HaversineDistance(startLat, startLng, (double)s.Stop.TS_Latitude, (double)s.Stop.TS_Longitude) / 1000.0
                }).OrderBy(x => x.TravelTime).ToList();

                var clusters = new List<List<(TrajectoryStop Stop, int Workers)>>();
                var used = new bool[items.Count];
                for (int i = 0; i < items.Count; i++)
                {
                    if (used[i]) continue;
                    var cluster = new List<(TrajectoryStop Stop, int Workers)>();
                    int currentWorkers = 0;
                    for (int j = 0; j < items.Count; j++)
                    {
                        if (used[j]) continue;
                        if (currentWorkers + items[j].Workers <= busCapacity)
                        {
                            cluster.Add((items[j].Stop, items[j].Workers));
                            currentWorkers += items[j].Workers;
                            used[j] = true;
                        }
                    }
                    if (cluster.Any())
                        clusters.Add(cluster);
                }

                var routes = new List<object>();
                foreach (var cluster in clusters)
                {
                    var stops = cluster.Select(c => new
                    {
                        c.Stop.TS_Id,
                        StopName = c.Stop.TS_Name,
                        PassengerCount = c.Workers
                    }).ToList();

                    var passengers = cluster.SelectMany(c => _context.Personnel
                        .Where(p => p.AssignedStopId == c.Stop.TS_Id && p.IsAssigned == true)
                        .Select(p => new { FirstName = p.Personnel_FirstName, LastName = p.Personnel_LastName })
                    ).ToList();

                    routes.Add(new
                    {
                        stops = stops,
                        passengers = passengers,
                        totalPassengers = stops.Sum(s => s.PassengerCount)
                    });
                }

                return Ok(new { success = true, message = $"{routes.Count} trajet(s) généré(s).", routes = routes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.ToString() });
            }
        }

        // ================================================
        // SAUVEGARDE DES TRAJETS (depuis modal)
        // ================================================
        [HttpPost]
        public async Task<IActionResult> SaveGeneratedRoutes([FromBody] SaveRoutesRequest request)
        {
            try
            {
                int busCapacity = request?.BusCapacity ?? 20;
                double speedKmh = 30;
                const double startLat = 34.2900, startLng = -6.5700;

                var stopsData = await _context.TrajectoryStops
                    .Where(s => s.TS_Latitude != 0 && s.TS_Longitude != 0)
                    .Select(s => new
                    {
                        Stop = s,
                        Workers = _context.Personnel.Count(p => p.AssignedStopId == s.TS_Id && p.IsAssigned == true)
                    })
                    .ToListAsync();

                var validStops = stopsData.Where(s => s.Workers > 0).ToList();
                if (!validStops.Any())
                    return BadRequest(new { success = false, message = "Aucun point de ramassage avec personnels assignés." });

                var items = validStops.Select(s => new
                {
                    Stop = s.Stop,
                    Workers = s.Workers,
                    TravelTime = HaversineDistance(startLat, startLng, (double)s.Stop.TS_Latitude, (double)s.Stop.TS_Longitude) / 1000.0 / speedKmh * 60.0
                }).OrderBy(x => x.TravelTime).ToList();

                var clusters = new List<List<(TrajectoryStop Stop, int Workers)>>();
                var used = new bool[items.Count];
                for (int i = 0; i < items.Count; i++)
                {
                    if (used[i]) continue;
                    var cluster = new List<(TrajectoryStop Stop, int Workers)>();
                    int currentWorkers = 0;
                    for (int j = 0; j < items.Count; j++)
                    {
                        if (used[j]) continue;
                        if (currentWorkers + items[j].Workers <= busCapacity)
                        {
                            cluster.Add((items[j].Stop, items[j].Workers));
                            currentWorkers += items[j].Workers;
                            used[j] = true;
                        }
                    }
                    if (cluster.Any())
                        clusters.Add(cluster);
                }

                var createdTrajs = new List<Trajectory>();
                int counter = 1;
                foreach (var cluster in clusters)
                {
                    var orderedStops = cluster.Select(c => c.Stop)
                        .OrderBy(s => HaversineDistance(startLat, startLng, (double)s.TS_Latitude, (double)s.TS_Longitude))
                        .ToList();

                    if (!orderedStops.Any()) continue;

                    double maxDist = orderedStops.Max(s => HaversineDistance(startLat, startLng, (double)s.TS_Latitude, (double)s.TS_Longitude) / 1000.0);
                    double maxTime = orderedStops.Max(s => HaversineDistance(startLat, startLng, (double)s.TS_Latitude, (double)s.TS_Longitude) / 1000.0 / speedKmh * 60.0);
                    int totalWorkers = cluster.Sum(c => c.Workers);

                    var traj = new Trajectory
                    {
                        Trajectory_Name = $"Trajet-{DateTime.Now:yyyyMMddHHmmss}-{counter}",
                        Trajectory_Code = $"T-{counter}",
                        Trajectory_Description = $"{orderedStops.Count} arrêts, {totalWorkers} pers",
                        Trajectory_StartLatitude = (decimal)startLat,
                        Trajectory_StartLongitude = (decimal)startLng,
                        Trajectory_EndLatitude = (decimal)orderedStops.Last().TS_Latitude,
                        Trajectory_EndLongitude = (decimal)orderedStops.Last().TS_Longitude,
                        Trajectory_DistanceKm = (decimal)Math.Round(maxDist, 2),
                        Trajectory_EstimatedDurationMinutes = (int)Math.Ceiling(maxTime),
                        Trajectory_Status = "Active",
                        Trajectory_CreatedAt = DateTime.Now,
                        Trajectory_UpdatedAt = DateTime.Now
                    };
                    _context.Trajectories.Add(traj);
                    await _context.SaveChangesAsync();

                    int order = 1;
                    foreach (var stop in orderedStops)
                    {
                        stop.TS_TrajectoryId = traj.Trajectory_Id;
                        stop.TS_OrderIndex = order++;
                        _context.TrajectoryStops.Update(stop);
                    }
                    await _context.SaveChangesAsync();
                    createdTrajs.Add(traj);
                    counter++;
                }

                return Ok(new { success = true, message = $"{createdTrajs.Count} trajets sauvegardés." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.ToString() });
            }
        }

        // ================================================
        // UTILITAIRES DE DISTANCE
        // ================================================
        private double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371e3; // mètres
            var φ1 = lat1 * Math.PI / 180;
            var φ2 = lat2 * Math.PI / 180;
            var Δφ = (lat2 - lat1) * Math.PI / 180;
            var Δλ = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
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
        // CLASSES AUXILIAIRES POUR LE CLUSTERING
        // ================================================
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
    public class CreateTrajectoryStopModel
    {
        public int TrajectoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
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

    public class GenerateRoutesRequest
    {
        public int BusCapacity { get; set; } = 20;
    }

    public class SaveRoutesRequest
    {
        public int BusCapacity { get; set; } = 20;
    }

    public class PickupRequest
    {
        public int TrajectoryId { get; set; }
        public int NumberOfClusters { get; set; } = 5;
    }
}