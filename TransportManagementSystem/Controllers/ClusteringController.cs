using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
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

    public class ClusterPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public object? Data { get; set; }
    }

    public class Cluster
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public List<ClusterPoint> Points { get; set; } = new List<ClusterPoint>();
    }

    // ================================================
    // CONTROLLER
    // ================================================
    public class ClusteringController : Controller
    {
        private readonly ApplicationDbContext _context;

        // KEEP YOUR CONSTRUCTOR (simpler, no OSRM dependency)
        public ClusteringController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<int> GetOrCreateDefaultTrajectoryId()
        {
            var defaultTrajectory = await _context.Trajectories.FirstOrDefaultAsync();
            if (defaultTrajectory != null)
            {
                return defaultTrajectory.Trajectory_Id;
            }

            var newTrajectory = new Trajectory
            {
                Trajectory_Name = "Trajectoire par défaut",
                Trajectory_Code = "T-DEFAULT",
                Trajectory_Description = "Trajectoire générée automatiquement",
                Trajectory_StartLatitude = 34.2900m,
                Trajectory_StartLongitude = -6.5700m,
                Trajectory_EndLatitude = 34.2900m,
                Trajectory_EndLongitude = -6.5700m,
                Trajectory_DistanceKm = 0,
                Trajectory_EstimatedDurationMinutes = 0,
                Trajectory_Status = "Active",
                Trajectory_CreatedAt = DateTime.Now,
                Trajectory_UpdatedAt = DateTime.Now
            };

            _context.Trajectories.Add(newTrajectory);
            await _context.SaveChangesAsync();
            return newTrajectory.Trajectory_Id;
        }

        public async Task<IActionResult> DispatcherView()
        {
            var stops = await _context.TrajectoryStops
                .Include(s => s.Trajectory)
                .OrderBy(s => s.TS_TrajectoryId)
                .ThenBy(s => s.TS_OrderIndex)
                .ToListAsync();
            return View(stops);
        }

        public async Task<IActionResult> DefinePickupPoints()
        {
            await GetOrCreateDefaultTrajectoryId();
            return View();
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
        public async Task<IActionResult> GetFilteredStops(int? trajectoryId)
        {
            IQueryable<TrajectoryStop> query = _context.TrajectoryStops.Include(s => s.Trajectory);
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

            return Ok(new { stopId = stop.TS_Id, stopName = stop.TS_Name, workers });
        }

        [HttpGet]
        public async Task<IActionResult> GetTrajectoriesWithStops()
        {
            var trajectories = await _context.Trajectories
                .Where(t => t.Trajectory_Status == "Active")
                .Select(t => new
                {
                    t.Trajectory_Id,
                    t.Trajectory_Name,
                    t.Trajectory_Code,
                    Stops = t.Stops.OrderBy(s => s.TS_OrderIndex).Select(s => new
                    {
                        s.TS_Id,
                        s.TS_Name,
                        s.TS_Latitude,
                        s.TS_Longitude,
                        s.TS_OrderIndex
                    }).ToList()
                })
                .ToListAsync();
            return Ok(trajectories);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnassignedWorkers()
        {
            var workers = await _context.Personnel
                .Where(p => p.IsAssigned == false && p.AssignedStopId == null && p.Personnel_Status == "Active"
                    && p.Personnel_Latitude != null && p.Personnel_Longitude != null)
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

            return Ok(new { success = true, message = $"Travailleur {worker.Personnel_FirstName} {worker.Personnel_LastName} assigné à l'arrêt {stop.TS_Name}" });
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

            return Ok(new { success = true, message = $"Travailleur {worker.Personnel_FirstName} {worker.Personnel_LastName} retiré de l'arrêt" });
        }

        [HttpPost]
        public async Task<IActionResult> AutoAssignWorkersToStop(int stopId)
        {
            var stop = await _context.TrajectoryStops.FindAsync(stopId);
            if (stop == null)
                return BadRequest(new { success = false, message = "Arrêt non trouvé" });

            var unassignedWorkers = await _context.Personnel
                .Where(p => p.IsAssigned == false && p.AssignedStopId == null && p.Personnel_Status == "Active"
                    && p.Personnel_Latitude != null && p.Personnel_Longitude != null)
                .ToListAsync();

            if (!unassignedWorkers.Any())
                return Ok(new { success = true, message = "Aucun personnel non assigné trouvé." });

            int assignedCount = 0;
            foreach (var worker in unassignedWorkers)
            {
                double distance = CalculateDistance((double)stop.TS_Latitude, (double)stop.TS_Longitude,
                    (double)worker.Personnel_Latitude!.Value, (double)worker.Personnel_Longitude!.Value);
                if (distance <= 50.0)
                {
                    worker.AssignedStopId = stopId;
                    worker.IsAssigned = true;
                    assignedCount++;
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = $"{assignedCount} personnel(s) assigné(s) à l'arrêt {stop.TS_Name}" });
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
                .Where(p => p.IsAssigned == false && p.AssignedStopId == null && p.Personnel_Status == "Active"
                    && p.Personnel_Latitude != null && p.Personnel_Longitude != null)
                .ToListAsync();

            if (!unassignedWorkers.Any())
                return Ok(new { success = true, message = "Aucun personnel non assigné trouvé." });

            int assignedCount = 0;
            foreach (var worker in unassignedWorkers)
            {
                TrajectoryStop? nearestStop = null;
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

        [HttpPost]
        public async Task<IActionResult> CreateStopForTrajectory([FromBody] CreateTrajectoryStopModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { success = false, message = "Nom invalide" });

            try
            {
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

            var personnel = await _context.Personnel.Where(p => p.AssignedStopId == id).ToListAsync();
            foreach (var p in personnel)
            {
                p.AssignedStopId = null;
                p.IsAssigned = false;
            }
            _context.TrajectoryStops.Remove(stop);
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

            return Ok(new { success = true, message = "Arrêt modifié" });
        }

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

            var personnelPoints = await _context.Personnel
                .Where(p => p.Personnel_Status == "Active" && p.Personnel_Latitude != null && p.Personnel_Longitude != null)
                .Select(p => new ClusterPoint
                {
                    X = (double)p.Personnel_Latitude!.Value,
                    Y = (double)p.Personnel_Longitude!.Value,
                    Data = new { p.Personnel_Id, p.Personnel_FirstName, p.Personnel_LastName }
                })
                .ToListAsync();

            if (personnelPoints.Count == 0)
                return Ok(new { clusters = new List<object>(), points = new List<object>(), message = "Aucun personnel avec coordonnées." });

            int k = Math.Max(1, Math.Min(request.NumberOfClusters, personnelPoints.Count));
            var clusters = KMeansDeterministic(personnelPoints, k);

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
                            double dist = CalculateDistance((double)centerLat, (double)centerLng, (double)stop.TS_Latitude, (double)stop.TS_Longitude);
                            return dist < 0.1;
                        });
                        uniqueClusters.Add(new { lat = centerLat, lng = centerLng, count = cluster.Points.Count, exists = exists });
                    }
                }
            }

            return Ok(new
            {
                clusters = uniqueClusters,
                points = personnelPoints.Select(p => new { lat = Math.Round((decimal)p.X, 6), lng = Math.Round((decimal)p.Y, 6) })
            });
        }

        private List<Cluster> KMeansDeterministic(List<ClusterPoint> points, int k, int maxIterations = 100)
        {
            if (points.Count == 0) return new List<Cluster>();
            if (k > points.Count) k = points.Count;

            Random rand = new Random(42);
            var distinctPoints = points.Select(p => new { p.X, p.Y }).Distinct().ToList();
            var centers = distinctPoints.OrderBy(x => rand.Next()).Take(k).Select(p => new { X = p.X, Y = p.Y }).ToList();
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

        [HttpPost]
        public async Task<IActionResult> GenerateRoutesFromPickupPoints([FromBody] GenerateRoutesRequest request)
        {
            try
            {
                int busCapacity = request?.BusCapacity ?? 20;

                var stopsWithWorkers = await _context.TrajectoryStops
                    .Where(s => s.TS_Latitude != 0 && s.TS_Longitude != 0)
                    .Select(s => new
                    {
                        Stop = s,
                        WorkerCount = _context.Personnel.Count(p => p.AssignedStopId == s.TS_Id && p.IsAssigned == true)
                    })
                    .ToListAsync();

                var validStops = stopsWithWorkers.Where(s => s.WorkerCount > 0).OrderBy(s => s.Stop.TS_OrderIndex).ToList();

                if (!validStops.Any())
                    return BadRequest(new { success = false, message = "Aucun point de ramassage avec personnels." });

                var routes = new List<object>();
                var currentRouteStops = new List<(TrajectoryStop Stop, int Workers)>();
                int currentTotal = 0;
                int routeNumber = 1;

                foreach (var stopInfo in validStops)
                {
                    int remaining = stopInfo.WorkerCount;
                    while (remaining > 0)
                    {
                        int canTake = busCapacity - currentTotal;
                        if (canTake == 0 && currentTotal == busCapacity)
                        {
                            if (currentRouteStops.Any())
                            {
                                routes.Add(CreateRouteObject(currentRouteStops, routeNumber));
                                routeNumber++;
                                currentRouteStops = new List<(TrajectoryStop Stop, int Workers)>();
                                currentTotal = 0;
                                canTake = busCapacity;
                            }
                        }

                        if (canTake > 0)
                        {
                            int take = Math.Min(remaining, canTake);
                            currentRouteStops.Add((stopInfo.Stop, take));
                            currentTotal += take;
                            remaining -= take;

                            if (currentTotal == busCapacity)
                            {
                                routes.Add(CreateRouteObject(currentRouteStops, routeNumber));
                                routeNumber++;
                                currentRouteStops = new List<(TrajectoryStop Stop, int Workers)>();
                                currentTotal = 0;
                            }
                        }
                    }
                }

                if (currentRouteStops.Any())
                {
                    routes.Add(CreateRouteObject(currentRouteStops, routeNumber));
                }

                return Ok(new { success = true, message = $"{routes.Count} trajet(s) généré(s).", routes = routes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.ToString() });
            }
        }

        private object CreateRouteObject(List<(TrajectoryStop Stop, int Workers)> routeStops, int routeNumber)
        {
            var stops = routeStops.Select((r, idx) => new
            {
                r.Stop.TS_Id,
                StopName = r.Stop.TS_Name,
                PassengerCount = r.Workers,
                Order = idx + 1,
                r.Stop.TS_Latitude,
                r.Stop.TS_Longitude
            }).ToList();

            var allPassengers = new List<object>();
            foreach (var r in routeStops)
            {
                var passengers = _context.Personnel
                    .Where(p => p.AssignedStopId == r.Stop.TS_Id && p.IsAssigned == true)
                    .Select(p => new { FirstName = p.Personnel_FirstName, LastName = p.Personnel_LastName })
                    .ToList();
                allPassengers.AddRange(passengers);
            }

            return new
            {
                stops = stops,
                passengers = allPassengers,
                totalPassengers = stops.Sum(s => s.PassengerCount),
                routeNumber = routeNumber
            };
        }

        [HttpPost]
        public async Task<IActionResult> SaveGeneratedRoutes([FromBody] SaveRoutesRequest request)
        {
            try
            {
                int busCapacity = request?.BusCapacity ?? 20;
                const double startLat = 34.2900, startLng = -6.5700;
                const double speedKmh = 30;

                var stopsWithWorkers = await _context.TrajectoryStops
                    .Where(s => s.TS_Latitude != 0 && s.TS_Longitude != 0)
                    .Select(s => new
                    {
                        Stop = s,
                        WorkerCount = _context.Personnel.Count(p => p.AssignedStopId == s.TS_Id && p.IsAssigned == true)
                    })
                    .ToListAsync();

                var validStops = stopsWithWorkers.Where(s => s.WorkerCount > 0).OrderBy(s => s.Stop.TS_OrderIndex).ToList();

                if (!validStops.Any())
                    return BadRequest(new { success = false, message = "Aucun point de ramassage avec personnels." });

                var createdTrajs = new List<Trajectory>();
                var currentRouteStops = new List<TrajectoryStop>();
                var currentRouteWorkers = new List<int>();
                int currentTotal = 0;
                int trajetCounter = 1;

                foreach (var stopInfo in validStops)
                {
                    int remaining = stopInfo.WorkerCount;
                    while (remaining > 0)
                    {
                        int canTake = busCapacity - currentTotal;
                        if (canTake == 0 && currentTotal == busCapacity)
                        {
                            if (currentRouteStops.Any())
                            {
                                await SaveTrajet(currentRouteStops, currentRouteWorkers, startLat, startLng, speedKmh, trajetCounter, createdTrajs);
                                currentRouteStops = new List<TrajectoryStop>();
                                currentRouteWorkers = new List<int>();
                                currentTotal = 0;
                                trajetCounter++;
                                canTake = busCapacity;
                            }
                        }

                        if (canTake > 0)
                        {
                            int take = Math.Min(remaining, canTake);
                            currentRouteStops.Add(stopInfo.Stop);
                            currentRouteWorkers.Add(take);
                            currentTotal += take;
                            remaining -= take;

                            if (currentTotal == busCapacity)
                            {
                                await SaveTrajet(currentRouteStops, currentRouteWorkers, startLat, startLng, speedKmh, trajetCounter, createdTrajs);
                                currentRouteStops = new List<TrajectoryStop>();
                                currentRouteWorkers = new List<int>();
                                currentTotal = 0;
                                trajetCounter++;
                            }
                        }
                    }
                }

                if (currentRouteStops.Any())
                {
                    await SaveTrajet(currentRouteStops, currentRouteWorkers, startLat, startLng, speedKmh, trajetCounter, createdTrajs);
                }

                return Ok(new { success = true, message = $"{createdTrajs.Count} trajets sauvegardés." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.ToString() });
            }
        }

        private async Task SaveTrajet(List<TrajectoryStop> stops, List<int> workers, double startLat, double startLng, double speedKmh, int counter, List<Trajectory> createdTrajs)
        {
            if (!stops.Any()) return;

            double totalDistance = 0;
            double currentLat = startLat;
            double currentLng = startLng;

            for (int i = 0; i < stops.Count; i++)
            {
                totalDistance += CalculateDistance(currentLat, currentLng, (double)stops[i].TS_Latitude, (double)stops[i].TS_Longitude);
                currentLat = (double)stops[i].TS_Latitude;
                currentLng = (double)stops[i].TS_Longitude;
            }

            double estimatedTime = totalDistance / speedKmh * 60;
            int totalWorkers = workers.Sum();

            var traj = new Trajectory
            {
                Trajectory_Name = $"Trajet-{DateTime.Now:yyyyMMddHHmmss}-{counter}",
                Trajectory_Code = $"T-{counter}",
                Trajectory_Description = $"{stops.Count} arrêts, {totalWorkers} pers",
                Trajectory_StartLatitude = (decimal)startLat,
                Trajectory_StartLongitude = (decimal)startLng,
                Trajectory_EndLatitude = (decimal)stops.Last().TS_Latitude,
                Trajectory_EndLongitude = (decimal)stops.Last().TS_Longitude,
                Trajectory_DistanceKm = (decimal)Math.Round(totalDistance, 2),
                Trajectory_EstimatedDurationMinutes = (int)Math.Ceiling(estimatedTime),
                Trajectory_Status = "Active",
                Trajectory_CreatedAt = DateTime.Now,
                Trajectory_UpdatedAt = DateTime.Now
            };

            _context.Trajectories.Add(traj);
            await _context.SaveChangesAsync();

            for (int i = 0; i < stops.Count; i++)
            {
                stops[i].TS_TrajectoryId = traj.Trajectory_Id;
                stops[i].TS_OrderIndex = i + 1;
                _context.TrajectoryStops.Update(stops[i]);
            }
            await _context.SaveChangesAsync();

            for (int i = 0; i < stops.Count; i++)
            {
                var stop = stops[i];
                int workersToTake = workers[i];

                var workersToAssign = await _context.Personnel
                    .Where(p => p.AssignedStopId == stop.TS_Id && p.IsAssigned == true)
                    .Take(workersToTake)
                    .ToListAsync();

                foreach (var worker in workersToAssign)
                {
                    worker.AssignedTrajectoryId = traj.Trajectory_Id;
                }
            }
            await _context.SaveChangesAsync();

            createdTrajs.Add(traj);
        }

        // ================================================
        // IA ÉVOLUTIONNAIRE POUR LA GÉNÉRATION DE TRAJETS OPTIMISÉS
        // ================================================
        [HttpPost]
        public async Task<IActionResult> GenerateSmartTrajectories()
        {
            try
            {
                const double startLat = 34.2900;
                const double startLng = -6.5700;
                const double speedKmh = 30;
                int busCapacity = 20;

                _context.ChangeTracker.Clear();

                var stopsData = await _context.TrajectoryStops
                    .AsNoTracking()
                    .Where(s => s.TS_Latitude != 0 && s.TS_Longitude != 0)
                    .Select(s => new
                    {
                        Stop = s,
                        WorkerCount = _context.Personnel.Count(p => p.AssignedStopId == s.TS_Id && p.IsAssigned == true)
                    })
                    .ToListAsync();

                var validStops = stopsData
                    .Where(s => s.WorkerCount > 0)
                    .Select(s => new { s.Stop, s.WorkerCount })
                    .OrderByDescending(s => CalculateDistance(startLat, startLng, (double)s.Stop.TS_Latitude, (double)s.Stop.TS_Longitude))
                    .ToList();

                if (!validStops.Any())
                    return Ok(new { success = false, message = "No pickup points with personnel found." });

                await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Transport].[TrajectoryStop_tbl] WHERE TS_TrajectoryId > 1");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Transport].[Trajectory_tbl] WHERE Trajectory_Id > 1");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('[Transport].[Trajectory_tbl]', RESEED, 1)");

                var stopList = validStops.Select(s => s.Stop).ToList();
                var remainingList = validStops.Select(s => s.WorkerCount).ToList();

                int trajectoryCounter = 1;
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

                while (remainingList.Any(r => r > 0))
                {
                    var currentTrajectoryStops = new List<(TrajectoryStop Stop, int WorkersToPick)>();
                    int currentLoad = 0;
                    var newRemainingList = remainingList.ToList();

                    for (int i = 0; i < stopList.Count; i++)
                    {
                        int remaining = remainingList[i];
                        if (remaining == 0) continue;

                        if (currentLoad + remaining <= busCapacity)
                        {
                            currentTrajectoryStops.Add((stopList[i], remaining));
                            currentLoad += remaining;
                            newRemainingList[i] = 0;
                        }
                        else if (currentLoad < busCapacity)
                        {
                            int take = busCapacity - currentLoad;
                            currentTrajectoryStops.Add((stopList[i], take));
                            currentLoad = busCapacity;
                            newRemainingList[i] = remaining - take;
                        }
                    }

                    for (int i = 0; i < remainingList.Count; i++)
                    {
                        remainingList[i] = newRemainingList[i];
                    }

                    if (currentTrajectoryStops.Any())
                    {
                        double totalDistance = 0;
                        double currentLat = startLat;
                        double currentLng = startLng;
                        foreach (var (stop, _) in currentTrajectoryStops)
                        {
                            totalDistance += CalculateDistance(currentLat, currentLng, (double)stop.TS_Latitude, (double)stop.TS_Longitude);
                            currentLat = (double)stop.TS_Latitude;
                            currentLng = (double)stop.TS_Longitude;
                        }

                        double estimatedTime = totalDistance / speedKmh * 60;
                        int totalWorkers = currentTrajectoryStops.Sum(t => t.WorkersToPick);

                        var traj = new Trajectory
                        {
                            Trajectory_Name = $"Trajet-{timestamp}-{trajectoryCounter}",
                            Trajectory_Code = $"T-{timestamp}-{trajectoryCounter}",
                            Trajectory_Description = $"{currentTrajectoryStops.Count} stops, {totalWorkers} workers",
                            Trajectory_StartLatitude = (decimal)startLat,
                            Trajectory_StartLongitude = (decimal)startLng,
                            Trajectory_EndLatitude = (decimal)currentTrajectoryStops.Last().Stop.TS_Latitude,
                            Trajectory_EndLongitude = (decimal)currentTrajectoryStops.Last().Stop.TS_Longitude,
                            Trajectory_DistanceKm = (decimal)Math.Round(totalDistance, 2),
                            Trajectory_EstimatedDurationMinutes = (int)Math.Ceiling(estimatedTime),
                            Trajectory_Status = "Active",
                            Trajectory_CreatedAt = DateTime.Now,
                            Trajectory_UpdatedAt = DateTime.Now
                        };

                        _context.Trajectories.Add(traj);
                        await _context.SaveChangesAsync();

                        for (int i = 0; i < currentTrajectoryStops.Count; i++)
                        {
                            var (stop, workersToPick) = currentTrajectoryStops[i];

                            var newStop = new TrajectoryStop
                            {
                                TS_TrajectoryId = traj.Trajectory_Id,
                                TS_Name = stop.TS_Name,
                                TS_OrderIndex = i + 1,
                                TS_Latitude = stop.TS_Latitude,
                                TS_Longitude = stop.TS_Longitude
                            };
                            _context.TrajectoryStops.Add(newStop);

                            var workersToAssign = await _context.Personnel
                                .Where(p => p.AssignedStopId == stop.TS_Id
                                    && p.IsAssigned == true
                                    && p.AssignedTrajectoryId == null)
                                .Take(workersToPick)
                                .ToListAsync();

                            foreach (var worker in workersToAssign)
                            {
                                worker.AssignedTrajectoryId = traj.Trajectory_Id;
                            }
                        }
                        await _context.SaveChangesAsync();

                        trajectoryCounter++;
                    }
                }

                var finalTrajectories = await _context.Trajectories.Where(t => t.Trajectory_Id > 1).CountAsync();
                var assignedWorkers = await _context.Personnel.CountAsync(p => p.AssignedTrajectoryId != null);

                return Ok(new
                {
                    success = true,
                    message = $"Generated {finalTrajectories} trajectories with {assignedWorkers} workers assigned.",
                    trajectoryCount = finalTrajectories,
                    workerCount = assignedWorkers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, innerException = ex.InnerException?.Message });
            }
        }

        // ================================================
        // ASSIGNATION AUTOMATIQUE DES BUS AUX TRAJETS (FRIEND'S VERSION - BETTER)
        // ================================================
        [HttpPost]
        public async Task<IActionResult> AutoAssignBusesToTrajectories()
        {
            try
            {
                var availableBuses = await _context.Buses
                    .Where(b => b.Bus_Status == "In Service" && b.Bus_CurrentTrajectoryId == null)
                    .ToListAsync();

                var trajectoriesWithoutBus = await _context.Trajectories
                    .Where(t => t.Trajectory_Status == "Active" && t.Trajectory_Id > 1 && !_context.Buses.Any(b => b.Bus_CurrentTrajectoryId == t.Trajectory_Id))
                    .OrderBy(t => t.Trajectory_Id)
                    .ToListAsync();

                if (!availableBuses.Any() || !trajectoriesWithoutBus.Any())
                    return Ok(new { success = false, message = "Aucun bus disponible ou aucune trajectoire sans bus." });

                int assignedCount = 0;
                for (int i = 0; i < Math.Min(availableBuses.Count, trajectoriesWithoutBus.Count); i++)
                {
                    var bus = availableBuses[i];
                    var trajectory = trajectoriesWithoutBus[i];

                    bus.Bus_CurrentTrajectoryId = trajectory.Trajectory_Id;

                    var stopsInTrajectory = await _context.TrajectoryStops
                        .Where(s => s.TS_TrajectoryId == trajectory.Trajectory_Id)
                        .OrderBy(s => s.TS_OrderIndex)
                        .ToListAsync();

                    int capacity = bus.Bus_Capacity ?? 20;
                    int currentOccupancy = 0;
                    int personnelAssigned = 0;

                    foreach (var stop in stopsInTrajectory)
                    {
                        if (currentOccupancy >= capacity) break;

                        var workersAtStop = await _context.Personnel
                            .Where(p => p.AssignedStopId == stop.TS_Id
                                        && p.AssignedBusId == null
                                        && p.Personnel_Status == "Active")
                            .ToListAsync();

                        foreach (var worker in workersAtStop)
                        {
                            if (currentOccupancy >= capacity) break;

                            worker.AssignedBusId = bus.Bus_Id;
                            worker.IsAssigned = true;
                            currentOccupancy++;
                            personnelAssigned++;
                        }
                    }

                    bus.CurrentOccupancy = currentOccupancy;
                    assignedCount++;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = $"{assignedCount} bus assignés aux trajectoires avec {_context.Personnel.Count(p => p.AssignedBusId != null)} personnels assignés." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
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
    }
}