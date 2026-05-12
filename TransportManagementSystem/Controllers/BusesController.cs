using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;
using TransportManagementSystem.Services;
using System;

namespace TransportManagementSystem.Controllers
{
    public class BusesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ETAPredictionService _etaService;

        public BusesController(ApplicationDbContext context, ETAPredictionService etaService)
        {
            _context = context;
            _etaService = etaService;
        }

        // ==============================
        // GESTION CRUD DES BUS
        // ==============================
        public async Task<IActionResult> Index()
        {
            var buses = await _context.Buses
                .Include(b => b.CurrentDriver)
                .ToListAsync();

            ViewBag.Drivers = await _context.Drivers
                .Where(d => d.Driver_Status == "Available" || d.Driver_Status == "On Route")
                .ToListAsync();

            return View(buses);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bus bus)
        {
            if (ModelState.IsValid)
            {
                bus.Bus_CreatedAt = DateTime.Now;
                bus.Bus_UpdatedAt = DateTime.Now;
                _context.Add(bus);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(bus);
        }

        [HttpGet]
        public async Task<IActionResult> GetBusData(long id)
        {
            var bus = await _context.Buses
                .Include(b => b.CurrentDriver)
                .FirstOrDefaultAsync(b => b.Bus_Id == id);
            if (bus == null) return NotFound();

            return Ok(new
            {
                bus.Bus_Id,
                bus.Bus_Code,
                bus.Bus_PlateNumber,
                bus.Bus_Brand,
                bus.Bus_Model,
                bus.Bus_Year,
                bus.Bus_Capacity,
                bus.Bus_Status,
                bus.Bus_CurrentDriverId
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBus([FromBody] BusUpdateModel model)
        {
            if (model == null || model.Bus_Id == 0)
                return BadRequest("Données invalides");

            var bus = await _context.Buses.FindAsync(model.Bus_Id);
            if (bus == null) return NotFound();

            bus.Bus_Code = model.Bus_Code;
            bus.Bus_PlateNumber = model.Bus_PlateNumber;
            bus.Bus_Brand = model.Bus_Brand;
            bus.Bus_Model = model.Bus_Model;
            bus.Bus_Year = model.Bus_Year;
            bus.Bus_Capacity = model.Bus_Capacity;
            bus.Bus_Status = model.Bus_Status;
            bus.Bus_CurrentDriverId = model.Bus_CurrentDriverId;
            bus.Bus_UpdatedAt = DateTime.Now;

            _context.Update(bus);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Bus mis à jour avec succès" });
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();
            var bus = await _context.Buses
                .Include(b => b.CurrentDriver)
                .FirstOrDefaultAsync(b => b.Bus_Id == id);
            if (bus == null) return NotFound();
            return View(bus);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Bus bus)
        {
            if (id != bus.Bus_Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    var existingBus = await _context.Buses.AsNoTracking().FirstOrDefaultAsync(b => b.Bus_Id == id);
                    if (existingBus == null) return NotFound();

                    bus.Bus_CurrentDriverId = existingBus.Bus_CurrentDriverId;
                    bus.Bus_CurrentLatitude = existingBus.Bus_CurrentLatitude;
                    bus.Bus_CurrentLongitude = existingBus.Bus_CurrentLongitude;
                    bus.Bus_LastLocationUpdateTime = existingBus.Bus_LastLocationUpdateTime;
                    bus.Bus_CreatedAt = existingBus.Bus_CreatedAt;
                    bus.Bus_UpdatedAt = DateTime.Now;

                    _context.Update(bus);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BusExists(bus.Bus_Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(bus);
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();
            var bus = await _context.Buses.FirstOrDefaultAsync(m => m.Bus_Id == id);
            if (bus == null) return NotFound();
            return View(bus);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var bus = await _context.Buses.FindAsync(id);
            if (bus != null) _context.Buses.Remove(bus);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BusExists(long id) => _context.Buses.Any(e => e.Bus_Id == id);

        [HttpGet]
        public async Task<IActionResult> GetBusLocations()
        {
            var buses = await _context.Buses
                .Where(b => b.Bus_CurrentLatitude != null && b.Bus_CurrentLongitude != null)
                .Select(b => new
                {
                    b.Bus_Id,
                    b.Bus_Code,
                    b.Bus_PlateNumber,
                    b.Bus_Status,
                    lat = b.Bus_CurrentLatitude,
                    lng = b.Bus_CurrentLongitude,
                    lastUpdate = b.Bus_LastLocationUpdateTime
                }).ToListAsync();
            return Ok(buses);
        }

        [HttpGet]
        public async Task<IActionResult> GetDriverDashboardData()
        {
            var driverIdStr = HttpContext.Session.GetString("DriverId");
            if (string.IsNullOrEmpty(driverIdStr))
                return Unauthorized();

            var driverId = long.Parse(driverIdStr);
            var driver = await _context.Drivers
                .Include(d => d.AssignedBus)
                .FirstOrDefaultAsync(d => d.Driver_id == driverId);

            if (driver?.AssignedBus == null)
                return NotFound("Aucun bus assigné.");

            var bus = driver.AssignedBus;

            return Ok(new
            {
                Driver = new { driver.Driver_id, driver.Driver_FirstName, driver.Driver_LastName },
                Bus = new
                {
                    bus.Bus_Id,
                    bus.Bus_Code,
                    bus.Bus_PlateNumber,
                    bus.Bus_Model,
                    bus.Bus_Brand,
                    bus.Bus_Status,
                    CurrentLatitude = bus.Bus_CurrentLatitude,
                    CurrentLongitude = bus.Bus_CurrentLongitude,
                    LastLocationUpdateTime = bus.Bus_LastLocationUpdateTime
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBusLocation([FromBody] LocationUpdateModel model)
        {
            var driverIdStr = HttpContext.Session.GetString("DriverId");
            if (string.IsNullOrEmpty(driverIdStr))
                return Unauthorized();

            var driverId = long.Parse(driverIdStr);
            var driver = await _context.Drivers
                .Include(d => d.AssignedBus)
                .FirstOrDefaultAsync(d => d.Driver_id == driverId);

            if (driver?.AssignedBus == null)
                return BadRequest("Aucun bus assigné.");

            var bus = driver.AssignedBus;
            bus.Bus_CurrentLatitude = model.Latitude;
            bus.Bus_CurrentLongitude = model.Longitude;
            bus.Bus_LastLocationUpdateTime = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // =================================================================
        // MÉTHODE POUR LE DASHBOARD PERSONNEL (sans fragments)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> GetPersonnelDashboardData()
        {
            var personnelIdStr = HttpContext.Session.GetString("PersonnelId");
            if (string.IsNullOrEmpty(personnelIdStr))
                return Unauthorized();

            var personnelId = long.Parse(personnelIdStr);
            var personnel = await _context.Personnel
                .Include(p => p.AssignedTrajectory)
                .Include(p => p.AssignedBus)
                    .ThenInclude(b => b.CurrentDriver)
                .FirstOrDefaultAsync(p => p.Personnel_Id == personnelId);

            if (personnel == null)
                return NotFound("Personnel non trouvé");

            // Si motorisé, retour simplifié
            if (personnel.IsMotorized)
            {
                return Ok(new
                {
                    isMotorized = true,
                    message = "Vous êtes motorisé"
                });
            }

            Trajectory? trajectory = personnel.AssignedTrajectory;
            TrajectoryStop? stop = null;
            string stopName = "Non défini";

            // Chercher via PersonnelTrajectoryAssignments
            if (trajectory == null)
            {
                var assignment = await _context.PersonnelTrajectoryAssignments
                    .Include(a => a.Trajectory)
                    .Include(a => a.Stop)
                    .FirstOrDefaultAsync(a => a.PTA_PersonnelId == personnelId && a.PTA_Status == "Active");
                if (assignment != null)
                {
                    trajectory = assignment.Trajectory;
                    stop = assignment.Stop;
                    if (stop != null)
                        stopName = stop.TS_Name;
                }
            }

            // Si arrêt direct dans personnel
            if (stop == null && personnel.AssignedStopId.HasValue)
            {
                stop = await _context.TrajectoryStops.FindAsync(personnel.AssignedStopId.Value);
                if (stop != null)
                {
                    stopName = stop.TS_Name;
                    if (trajectory == null)
                        trajectory = await _context.Trajectories.FindAsync(stop.TS_TrajectoryId);
                }
            }

            if (trajectory == null)
                return NotFound("Aucune trajectoire assignée.");

            // Récupérer le bus assigné et son chauffeur
            Bus? assignedBus = personnel.AssignedBus;
            string driverName = "Non assigné";
            if (assignedBus?.CurrentDriver != null)
                driverName = $"{assignedBus.CurrentDriver.Driver_FirstName} {assignedBus.CurrentDriver.Driver_LastName}";

            // Récupérer tous les bus actifs sur cette trajectoire (via la trajectoire, pas via fragments)
            var buses = await _context.Buses
                .Where(b => b.Bus_CurrentLatitude != null && b.Bus_CurrentLongitude != null)
                .Select(b => new { b.Bus_Id, b.Bus_Code, b.Bus_PlateNumber, b.Bus_Status, lat = b.Bus_CurrentLatitude, lng = b.Bus_CurrentLongitude, b.Bus_LastLocationUpdateTime })
                .ToListAsync();

            // Récupérer tous les arrêts de la trajectoire
            var stopsList = await _context.TrajectoryStops
                .Where(s => s.TS_TrajectoryId == trajectory.Trajectory_Id)
                .OrderBy(s => s.TS_OrderIndex)
                .Select(s => new { s.TS_Id, s.TS_Name, s.TS_OrderIndex, s.TS_Latitude, s.TS_Longitude })
                .ToListAsync();

            // Coordonnées de référence pour alertes (l'arrêt du personnel ou début trajectoire)
            double? refLat = null;
            double? refLng = null;
            if (stop != null)
            {
                refLat = (double)stop.TS_Latitude;
                refLng = (double)stop.TS_Longitude;
            }
            else if (trajectory.Trajectory_StartLatitude.HasValue && trajectory.Trajectory_StartLongitude.HasValue)
            {
                refLat = (double)trajectory.Trajectory_StartLatitude.Value;
                refLng = (double)trajectory.Trajectory_StartLongitude.Value;
            }

            // Calculer ETA pour chaque bus (optionnel, garde le service)
            var busesWithETA = new List<object>();
            if (refLat.HasValue && refLng.HasValue)
            {
                foreach (var bus in buses)
                {
                    double busLat = bus.lat.HasValue ? (double)bus.lat.Value : 0;
                    double busLng = bus.lng.HasValue ? (double)bus.lng.Value : 0;
                    double distance = CalculateDistance(refLat.Value, refLng.Value, busLat, busLng);
                    float eta = _etaService.PredictETA((float)(distance / 1000.0), DateTime.Now);
                    busesWithETA.Add(new
                    {
                        bus.Bus_Id,
                        bus.Bus_Code,
                        bus.Bus_PlateNumber,
                        bus.Bus_Status,
                        lat = bus.lat,
                        lng = bus.lng,
                        bus.Bus_LastLocationUpdateTime,
                        etaMinutes = Math.Round(eta)
                    });
                }
            }
            else
            {
                foreach (var bus in buses)
                {
                    busesWithETA.Add(new
                    {
                        bus.Bus_Id,
                        bus.Bus_Code,
                        bus.Bus_PlateNumber,
                        bus.Bus_Status,
                        lat = bus.lat,
                        lng = bus.lng,
                        bus.Bus_LastLocationUpdateTime
                    });
                }
            }

            return Ok(new
            {
                personnelId = personnelId,
                trajectory = new
                {
                    trajectory.Trajectory_Id,
                    trajectory.Trajectory_Name,
                    trajectory.Trajectory_Code,
                    distance = trajectory.Trajectory_DistanceKm,
                    estimatedDuration = trajectory.Trajectory_EstimatedDurationMinutes,
                    startLat = trajectory.Trajectory_StartLatitude,
                    startLng = trajectory.Trajectory_StartLongitude,
                    endLat = trajectory.Trajectory_EndLatitude,
                    endLng = trajectory.Trajectory_EndLongitude
                },
                personnelStop = stop != null ? new { stop.TS_Id, stop.TS_Name, stop.TS_Latitude, stop.TS_Longitude } : null,
                stops = stopsList,
                buses = busesWithETA,
                assignedBus = assignedBus != null ? new
                {
                    assignedBus.Bus_Id,
                    assignedBus.Bus_Code,
                    assignedBus.Bus_PlateNumber,
                    assignedBus.Bus_Brand,
                    assignedBus.Bus_Model,
                    assignedBus.Bus_Capacity
                } : null,
                assignedDriver = driverName,
                stopName = stopName,
                isMotorized = false
            });
        }

        [HttpPost]
        public async Task<IActionResult> CheckProximityAlert([FromBody] ProximityCheckModel model)
        {
            var personnelIdStr = HttpContext.Session.GetString("PersonnelId");
            if (string.IsNullOrEmpty(personnelIdStr))
                return Unauthorized();

            var personnelId = long.Parse(personnelIdStr);
            var lastAlert = await _context.Alerts
                .Where(a => a.Alert_PersonnelId == personnelId && a.Alert_BusId == model.BusId)
                .OrderByDescending(a => a.Alert_SentAt)
                .FirstOrDefaultAsync();

            string? alertType = null;
            if (model.Distance <= 200 && (lastAlert?.Alert_Type != "200m"))
                alertType = "200m";
            else if (model.Distance <= 500 && (lastAlert?.Alert_Type != "200m" && lastAlert?.Alert_Type != "500m"))
                alertType = "500m";

            if (!string.IsNullOrEmpty(alertType))
            {
                var alert = new Alert
                {
                    Alert_PersonnelId = personnelId,
                    Alert_BusId = model.BusId,
                    Alert_TrajectoryId = model.TrajectoryId,
                    Alert_Type = alertType,
                    Alert_Message = $"Le bus {model.BusCode} est à {model.Distance:F0} mètres de votre arrêt.",
                    Alert_SentAt = DateTime.Now,
                    Alert_DeliveryChannel = "Web",
                    Alert_Status = "sent"
                };
                _context.Alerts.Add(alert);
                await _context.SaveChangesAsync();
                return Ok(new { alertType, message = alert.Alert_Message });
            }
            return Ok(new { alertType = "none" });
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371e3;
            var φ1 = lat1 * Math.PI / 180;
            var φ2 = lat2 * Math.PI / 180;
            var Δφ = (lat2 - lat1) * Math.PI / 180;
            var Δλ = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) + Math.Cos(φ1) * Math.Cos(φ2) * Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }

    public class ProximityCheckModel
    {
        public long BusId { get; set; }
        public string BusCode { get; set; } = string.Empty;
        public int TrajectoryId { get; set; }
        public double Distance { get; set; }
    }

    public class BusUpdateModel
    {
        public long Bus_Id { get; set; }
        public string Bus_Code { get; set; } = string.Empty;
        public string Bus_PlateNumber { get; set; } = string.Empty;
        public string Bus_Brand { get; set; } = string.Empty;
        public string Bus_Model { get; set; } = string.Empty;
        public int? Bus_Year { get; set; }
        public int? Bus_Capacity { get; set; }
        public string Bus_Status { get; set; } = string.Empty;
        public long? Bus_CurrentDriverId { get; set; }
    }
}