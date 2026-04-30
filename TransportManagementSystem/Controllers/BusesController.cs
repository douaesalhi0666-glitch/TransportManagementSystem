using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class BusesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BusesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==============================
        // GESTION CRUD DES BUS
        // ==============================
        public async Task<IActionResult> Index()
        {
            var buses = await _context.Buses
                .Include(b => b.CurrentDriver)
                .Include(b => b.CurrentTrajectory)
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

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();
            var bus = await _context.Buses.FindAsync(id);
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

        // ========================================================
        // API POUR LES CARTES (position des bus)
        // ========================================================
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

        // ========================================================
        // API POUR LE TABLEAU DE BORD DU DRIVER
        // ========================================================
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
            var trajectoryId = bus.Bus_CurrentTrajectoryId;
            Trajectory? trajectory = null;
            var stops = new System.Collections.Generic.List<object>();

            if (trajectoryId.HasValue)
            {
                trajectory = await _context.Trajectories
                    .FirstOrDefaultAsync(t => t.Trajectory_Id == trajectoryId);
                if (trajectory != null)
                {
                    stops = await _context.TrajectoryStops
                        .Where(s => s.TS_TrajectoryId == trajectoryId)
                        .OrderBy(s => s.TS_OrderIndex)
                        .Select(s => new
                        {
                            s.TS_Id,
                            s.TS_Name,
                            s.TS_OrderIndex,
                            s.TS_Latitude,
                            s.TS_Longitude,
                            s.TS_PlannedArrivalTime,
                            s.TS_PlannedDepartureTime
                        })
                        .ToListAsync<object>();
                }
            }

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
                },
                Trajectory = trajectory != null ? new
                {
                    trajectory.Trajectory_Id,
                    trajectory.Trajectory_Name,
                    trajectory.Trajectory_Code,
                    StartLatitude = trajectory.Trajectory_StartLatitude,
                    StartLongitude = trajectory.Trajectory_StartLongitude,
                    EndLatitude = trajectory.Trajectory_EndLatitude,
                    EndLongitude = trajectory.Trajectory_EndLongitude
                } : null,
                Stops = stops
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

        // ========================================================
        // API POUR LE TABLEAU DE BORD DU PERSONNEL (CORRIGÉE)
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> GetPersonnelDashboardData()
        {
            var personnelIdStr = HttpContext.Session.GetString("PersonnelId");
            if (string.IsNullOrEmpty(personnelIdStr))
                return Unauthorized();

            var personnelId = long.Parse(personnelIdStr);

            // Récupérer le personnel avec ses assignations directes (AssignedTrajectory, AssignedBus)
            var personnel = await _context.Personnel
                .Include(p => p.AssignedTrajectory)
                .Include(p => p.AssignedBus)
                .FirstOrDefaultAsync(p => p.Personnel_Id == personnelId);

            Trajectory? trajectory = null;
            TrajectoryStop? stop = null;

            // 1. Vérifier s'il y a une assignation directe (via AssignedTrajectoryId)
            if (personnel?.AssignedTrajectory != null && personnel.AssignedBus != null)
            {
                trajectory = personnel.AssignedTrajectory;
                // Optionnel : récupérer un arrêt spécifique si vous avez un champ PersonnelStopId
                // stop = await _context.TrajectoryStops.FirstOrDefaultAsync(s => s.TS_Id == personnel.PersonnelStopId);
            }
            else
            {
                // 2. Sinon, chercher dans la table PersonnelTrajectoryAssignments (ancienne méthode)
                var assignment = await _context.PersonnelTrajectoryAssignments
                    .Include(a => a.Trajectory)
                    .Include(a => a.Stop)
                    .FirstOrDefaultAsync(a => a.PTA_PersonnelId == personnelId
                                              && a.PTA_Status == "Active"
                                              && a.PTA_EffectiveFromDate <= DateTime.Now
                                              && (a.PTA_EffectiveToDate == null || a.PTA_EffectiveToDate >= DateTime.Now));
                if (assignment == null)
                    return NotFound("Aucune trajectoire assignée.");

                trajectory = assignment.Trajectory;
                stop = assignment.Stop;
            }

            if (trajectory == null)
                return NotFound("Trajectoire introuvable.");

            // Bus actifs sur cette trajectoire
            var buses = await _context.Buses
                .Where(b => b.Bus_CurrentTrajectoryId == trajectory.Trajectory_Id
                            && b.Bus_CurrentLatitude != null
                            && b.Bus_CurrentLongitude != null)
                .Select(b => new
                {
                    b.Bus_Id,
                    b.Bus_Code,
                    b.Bus_PlateNumber,
                    b.Bus_Status,
                    lat = b.Bus_CurrentLatitude,
                    lng = b.Bus_CurrentLongitude,
                    b.Bus_LastLocationUpdateTime
                }).ToListAsync();

            // Tous les arrêts de la trajectoire
            var stops = await _context.TrajectoryStops
                .Where(s => s.TS_TrajectoryId == trajectory.Trajectory_Id)
                .OrderBy(s => s.TS_OrderIndex)
                .Select(s => new
                {
                    s.TS_Id,
                    s.TS_Name,
                    s.TS_OrderIndex,
                    s.TS_Latitude,
                    s.TS_Longitude,
                    s.TS_PlannedArrivalTime,
                    s.TS_PlannedDepartureTime
                }).ToListAsync();

            var personnelStop = stop != null ? new
            {
                stop.TS_Id,
                stop.TS_Name,
                stop.TS_Latitude,
                stop.TS_Longitude
            } : null;

            return Ok(new
            {
                PersonnelId = personnelId,
                Trajectory = new
                {
                    trajectory.Trajectory_Id,
                    trajectory.Trajectory_Name,
                    trajectory.Trajectory_Code,
                    StartLat = trajectory.Trajectory_StartLatitude,
                    StartLng = trajectory.Trajectory_StartLongitude,
                    EndLat = trajectory.Trajectory_EndLatitude,
                    EndLng = trajectory.Trajectory_EndLongitude
                },
                PersonnelStop = personnelStop,
                Stops = stops,
                Buses = buses
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
    }

    // ========================================================
    // MODÈLES INTERNES (DTO)
    // ========================================================
    public class LocationUpdateModel
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }

    public class ProximityCheckModel
    {
        public long BusId { get; set; }
        public string BusCode { get; set; } = string.Empty;
        public int TrajectoryId { get; set; }
        public double Distance { get; set; }
    }
}