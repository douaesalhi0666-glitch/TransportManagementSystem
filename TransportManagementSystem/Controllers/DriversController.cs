using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class DriversController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DriversController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var drivers = await _context.Drivers
                .Include(d => d.AssignedBus)
                .ToListAsync();

            ViewBag.Buses = await _context.Buses.ToListAsync();

            return View(drivers);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Driver driver)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.Drivers.FindAsync(driver.Driver_id);
                if (existing != null)
                {
                    ModelState.AddModelError("Driver_id", "Cet ID existe déjà.");
                    return View(driver);
                }

                driver.Driver_CreatedAt = DateTime.Now;
                driver.Driver_UpdatedAt = DateTime.Now;
                driver.Driver_HireDate = DateTime.Now; // Date d'entrée automatique
                driver.Driver_Rating = "Bon"; // Valeur par défaut
                driver.Driver_Status = "Available";

                _context.Add(driver);
                await _context.SaveChangesAsync();

                DashboardController.AddNotification("success", "Chauffeur ajouté", $"Le chauffeur {driver.Driver_FirstName} {driver.Driver_LastName} a été ajouté avec succès.");

                return RedirectToAction(nameof(Index));
            }
            return View(driver);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var driver = await _context.Drivers
                .Include(d => d.AssignedBus)
                .FirstOrDefaultAsync(d => d.Driver_id == id);

            if (driver == null) return NotFound();

            ViewBag.Buses = await _context.Buses.ToListAsync();

            return View(driver);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Driver driver)
        {
            if (id != driver.Driver_id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDriver = await _context.Drivers
                        .AsNoTracking()
                        .Include(d => d.AssignedBus)
                        .FirstOrDefaultAsync(d => d.Driver_id == id);

                    if (existingDriver != null)
                    {
                        if (existingDriver.Driver_AssignedBusId != null &&
                            driver.Driver_Status == "Off Duty" &&
                            existingDriver.Driver_Status != "Off Duty")
                        {
                            var bus = await _context.Buses.FindAsync(existingDriver.Driver_AssignedBusId.Value);
                            if (bus != null)
                            {
                                bus.Bus_CurrentDriverId = null;
                                TempData["Warning"] = $"⚠️ Attention: Le chauffeur a été retiré du bus {bus.Bus_Code}. Ce bus n'a plus de chauffeur.";
                                DashboardController.AddNotification("warning", "Chauffeur retiré du bus", $"Le chauffeur {driver.Driver_FirstName} {driver.Driver_LastName} a été retiré du bus {bus.Bus_Code}.");
                            }
                            driver.Driver_AssignedBusId = null;
                        }

                        driver.Driver_CreatedAt = existingDriver.Driver_CreatedAt;
                        driver.Driver_UpdatedAt = DateTime.Now;
                        driver.Driver_HireDate = existingDriver.Driver_HireDate;

                        _context.Update(driver);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DriverExists(driver.Driver_id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Buses = await _context.Buses.ToListAsync();
            return View(driver);
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            var driver = await _context.Drivers
                .Include(d => d.AssignedBus)
                .FirstOrDefaultAsync(m => m.Driver_id == id);
            if (driver == null) return NotFound();

            return View(driver);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver != null)
            {
                string driverName = $"{driver.Driver_FirstName} {driver.Driver_LastName}";

                if (driver.Driver_AssignedBusId != null)
                {
                    var bus = await _context.Buses.FindAsync(driver.Driver_AssignedBusId.Value);
                    if (bus != null)
                    {
                        bus.Bus_CurrentDriverId = null;
                        DashboardController.AddNotification("warning", "Chauffeur supprimé avec bus", $"Le chauffeur {driverName} a été supprimé. Le bus {bus.Bus_Code} est maintenant sans chauffeur.");
                    }
                }
                else
                {
                    DashboardController.AddNotification("delete", "Chauffeur supprimé", $"Le chauffeur {driverName} a été supprimé de la base.");
                }

                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentStatus()
        {
            var driverIdStr = HttpContext.Session.GetString("DriverId");
            if (string.IsNullOrEmpty(driverIdStr))
            {
                return Json(new { statusText = "Non connecté", statusClass = "bg-secondary" });
            }

            var driverId = long.Parse(driverIdStr);
            var newStatus = await CalculateDriverStatus(driverId);

            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver != null && driver.Driver_Status != newStatus)
            {
                driver.Driver_Status = newStatus;
                driver.Driver_UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            string statusText = "";
            string statusClass = "";

            switch (newStatus)
            {
                case "On Route":
                    statusText = "🔄 En route";
                    statusClass = "bg-warning";
                    break;
                case "Available":
                    statusText = "✅ Disponible";
                    statusClass = "bg-success";
                    break;
                case "Off Duty":
                    statusText = "🔴 Hors service";
                    statusClass = "bg-danger";
                    break;
                default:
                    statusText = newStatus;
                    statusClass = "bg-secondary";
                    break;
            }

            return Json(new { statusText = statusText, statusClass = statusClass });
        }

        [HttpPost]
        public async Task<IActionResult> CompleteMission()
        {
            var driverIdStr = HttpContext.Session.GetString("DriverId");
            if (string.IsNullOrEmpty(driverIdStr))
            {
                return Json(new { success = false, message = "Non connecté" });
            }

            var driverId = long.Parse(driverIdStr);
            var driver = await _context.Drivers.FindAsync(driverId);

            if (driver == null)
            {
                return Json(new { success = false, message = "Chauffeur non trouvé" });
            }

            driver.Driver_Status = "Off Duty";
            driver.Driver_UpdatedAt = DateTime.Now;

            var mission = await _context.DriverMissions_tbl
                .Where(m => m.Driver_Id == driverId && m.Mission_Date == DateTime.Now.Date)
                .FirstOrDefaultAsync();

            if (mission == null)
            {
                mission = new DriverMission
                {
                    Driver_Id = driverId,
                    Bus_Id = driver.Driver_AssignedBusId ?? 0,
                    Mission_Date = DateTime.Now.Date,
                    StartTime = DateTime.Now,
                    Status = "Completed"
                };
                _context.DriverMissions_tbl.Add(mission);
            }
            else
            {
                mission.EndTime = DateTime.Now;
                mission.Status = "Completed";
            }

            await _context.SaveChangesAsync();

            DashboardController.AddNotification("success", "Mission terminée", $"Le chauffeur {driver.Driver_FirstName} {driver.Driver_LastName} a terminé sa mission.");

            return Json(new { success = true, message = "Mission terminée. Bonne soirée !" });
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            var driverIdStr = HttpContext.Session.GetString("DriverId");
            if (string.IsNullOrEmpty(driverIdStr))
                return Unauthorized();

            var driverId = long.Parse(driverIdStr);
            var driver = await _context.Drivers
                .Include(d => d.AssignedBus)
                    .ThenInclude(b => b.CurrentTrajectory)  // ← IMPORTANT: Include trajectory
                .FirstOrDefaultAsync(d => d.Driver_id == driverId);

            if (driver?.AssignedBus == null)
            {
                return Ok(new
                {
                    hasBus = false,
                    message = "Aucun bus assigné"
                });
            }

            var bus = driver.AssignedBus;
            var trajectory = bus.CurrentTrajectory;

            // Get stops for the trajectory (if assigned)
            List<object> stops = new List<object>();
            if (trajectory != null)
            {
                stops = await _context.TrajectoryStops
                    .Where(s => s.TS_TrajectoryId == trajectory.Trajectory_Id)
                    .OrderBy(s => s.TS_OrderIndex)
                    .Select(s => new
                    {
                        s.TS_Id,
                        s.TS_Name,
                        s.TS_OrderIndex,
                        s.TS_Latitude,
                        s.TS_Longitude,
                        workers_At_Stop = _context.Personnel.Count(p => p.AssignedStopId == s.TS_Id && p.IsAssigned == true)
                    })
                    .ToListAsync<object>();
            }

            return Ok(new
            {
                hasBus = true,
                bus = new
                {
                    bus.Bus_Id,
                    bus.Bus_Code,
                    bus.Bus_PlateNumber,
                    bus.Bus_Brand,
                    bus.Bus_Model,
                    bus.Bus_Status,
                    currentLatitude = bus.Bus_CurrentLatitude ?? 0,
                    currentLongitude = bus.Bus_CurrentLongitude ?? 0
                },
                trajectory = trajectory != null ? new
                {
                    trajectory.Trajectory_Id,
                    trajectory.Trajectory_Name,
                    trajectory.Trajectory_Code,
                    trajectory.Trajectory_Description
                } : null,
                stops = stops
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

        [HttpGet]
        public async Task<IActionResult> GetDriverData(long id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null) return NotFound();

            return Ok(new
            {
                driver.Driver_id,
                driver.Driver_FirstName,
                driver.Driver_LastName,
                driver.Driver_Email,
                driver.Driver_PhoneNumber,
                driver.Driver_LicenseNumber,
                driver.Driver_LicenseExpiryDate,
                driver.Driver_HireDate,
                driver.Driver_Rating,
                driver.Driver_Status,
                driver.Driver_AssignedBusId
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDriver([FromBody] DriverUpdateModel model)
        {
            if (model == null || model.Driver_id == 0)
                return BadRequest(new { success = false, message = "Données invalides" });

            var driver = await _context.Drivers.FindAsync(model.Driver_id);
            if (driver == null)
                return NotFound(new { success = false, message = "Chauffeur non trouvé" });

            driver.Driver_FirstName = model.Driver_FirstName;
            driver.Driver_LastName = model.Driver_LastName;
            driver.Driver_Email = model.Driver_Email;
            driver.Driver_PhoneNumber = model.Driver_PhoneNumber;
            driver.Driver_LicenseNumber = model.Driver_LicenseNumber;
            driver.Driver_LicenseExpiryDate = model.Driver_LicenseExpiryDate;
            driver.Driver_Rating = model.Driver_Rating;

            if (driver.Driver_AssignedBusId != model.Driver_AssignedBusId)
            {
                if (driver.Driver_AssignedBusId.HasValue)
                {
                    var oldBus = await _context.Buses.FindAsync(driver.Driver_AssignedBusId.Value);
                    if (oldBus != null)
                        oldBus.Bus_CurrentDriverId = null;
                }

                if (model.Driver_AssignedBusId.HasValue)
                {
                    var newBus = await _context.Buses.FindAsync(model.Driver_AssignedBusId.Value);
                    if (newBus != null && newBus.Bus_CurrentDriverId == null)
                    {
                        newBus.Bus_CurrentDriverId = driver.Driver_id;
                        DashboardController.AddNotification("assignment", "Chauffeur assigné", $"Le chauffeur {driver.Driver_FirstName} {driver.Driver_LastName} a été assigné au bus {newBus.Bus_Code}.");
                    }
                    else if (newBus != null && newBus.Bus_CurrentDriverId != null)
                        return BadRequest(new { success = false, message = "Ce bus a déjà un chauffeur." });
                }
                driver.Driver_AssignedBusId = model.Driver_AssignedBusId;
            }

            driver.Driver_UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Chauffeur mis à jour avec succès." });
        }

        // ========== NOUVELLES MÉTHODES ==========

        [HttpGet]
        public async Task<IActionResult> GetNextId()
        {
            var maxId = await _context.Drivers.MaxAsync(d => (long?)d.Driver_id) ?? 0;
            var nextId = maxId + 1;
            return Ok(new { nextId = nextId });
        }

        [HttpGet]
        public async Task<IActionResult> CheckIdExists(long id)
        {
            var exists = await _context.Drivers.AnyAsync(d => d.Driver_id == id);
            return Ok(new { exists = exists });
        }

        private async Task<string> CalculateDriverStatus(long driverId)
        {
            var now = DateTime.Now;
            var currentHour = now.Hour;
            var currentDay = now.DayOfWeek;

            // Week-end : Hors service toute la journée
            if (currentDay == DayOfWeek.Saturday || currentDay == DayOfWeek.Sunday)
            {
                return "Off Duty";
            }

            // Lundi au Vendredi
            if (currentHour >= 7 && currentHour < 8)
            {
                return "On Route";
            }
            else if (currentHour >= 8 && currentHour < 17)
            {
                return "Available";
            }
            else if (currentHour >= 17 && currentHour < 18)
            {
                return "On Route";
            }
            else
            {
                return "Off Duty";
            }
        }

        private bool DriverExists(long id) => _context.Drivers.Any(e => e.Driver_id == id);
    }

    public class DriverUpdateModel
    {
        public long Driver_id { get; set; }
        public string Driver_FirstName { get; set; } = string.Empty;
        public string Driver_LastName { get; set; } = string.Empty;
        public string? Driver_Email { get; set; }
        public string? Driver_PhoneNumber { get; set; }
        public string Driver_LicenseNumber { get; set; } = string.Empty;
        public DateTime? Driver_LicenseExpiryDate { get; set; }
        public string? Driver_Rating { get; set; }
        public long? Driver_AssignedBusId { get; set; }
    }

    public class LocationUpdateModel
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}