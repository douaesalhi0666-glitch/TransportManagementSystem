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

        // ==============================
        // GESTION CRUD DES CHAUFFEURS
        // ==============================

        public async Task<IActionResult> Index()
        {
            var drivers = await _context.Drivers
                .Include(d => d.AssignedBus)
                .ToListAsync();
            return View(drivers);
        }

        public IActionResult Create() => View();

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
                driver.Driver_Status = "Available";

                _context.Add(driver);
                await _context.SaveChangesAsync();
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

            if (driver.Driver_AssignedBusId != null)
                ViewBag.Warning = $"⚠️ Ce chauffeur est actuellement assigné au bus {driver.AssignedBus?.Bus_Code}. Si vous le mettez hors service, ce bus n'aura plus de chauffeur.";
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
                        if (existingDriver.Driver_AssignedBusId != null && driver.Driver_Status == "Off Duty" && existingDriver.Driver_Status != "Off Duty")
                        {
                            var bus = await _context.Buses.FindAsync(existingDriver.Driver_AssignedBusId);
                            if (bus != null)
                            {
                                bus.Bus_CurrentDriverId = null;
                                TempData["Warning"] = $"⚠️ Attention: Le chauffeur a été retiré du bus {bus.Bus_Code}. Ce bus n'a plus de chauffeur.";
                            }
                            driver.Driver_AssignedBusId = null;
                        }

                        driver.Driver_CreatedAt = existingDriver.Driver_CreatedAt;
                        driver.Driver_UpdatedAt = DateTime.Now;

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
                if (driver.Driver_AssignedBusId != null)
                {
                    var bus = await _context.Buses.FindAsync(driver.Driver_AssignedBusId);
                    if (bus != null) bus.Bus_CurrentDriverId = null;
                }
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DriverExists(long id) => _context.Drivers.Any(e => e.Driver_id == id);

        // ========================================================
        // API POUR LE TABLEAU DE BORD DU CHAUFFEUR
        // ========================================================

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            var driverIdStr = HttpContext.Session.GetString("DriverId");
            if (string.IsNullOrEmpty(driverIdStr))
                return Unauthorized();

            var driverId = long.Parse(driverIdStr);
            var driver = await _context.Drivers
                .Include(d => d.AssignedBus)
                    .ThenInclude(b => b.CurrentTrajectory)
                .FirstOrDefaultAsync(d => d.Driver_id == driverId);

            if (driver?.AssignedBus == null)
                return Ok(new { hasBus = false });

            var bus = driver.AssignedBus;
            var trajectory = bus.CurrentTrajectory;
            var stops = new List<object>();
            if (trajectory != null)
            {
                stops = await _context.TrajectoryStops
                    .Where(s => s.TS_TrajectoryId == trajectory.Trajectory_Id)
                    .OrderBy(s => s.TS_OrderIndex)
                    .Select(s => new
                    {
                        s.TS_OrderIndex,
                        s.TS_Name,
                        Latitude = s.TS_Latitude,
                        Longitude = s.TS_Longitude
                    })
                    .ToListAsync<object>();
            }

            return Ok(new
            {
                hasBus = true,
                bus = new
                {
                    bus.Bus_Code,
                    bus.Bus_PlateNumber,
                    bus.Bus_Model,
                    bus.Bus_Brand,
                    bus.Bus_Status,
                    currentLatitude = bus.Bus_CurrentLatitude,
                    currentLongitude = bus.Bus_CurrentLongitude
                },
                trajectory = trajectory != null ? new
                {
                    trajectory.Trajectory_Id,
                    trajectory.Trajectory_Name,
                    trajectory.Trajectory_Code
                } : null,
                stops = stops
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentStatus()
        {
            var driverIdStr = HttpContext.Session.GetString("DriverId");
            if (string.IsNullOrEmpty(driverIdStr))
                return Json(new { statusText = "Non connecté", statusClass = "bg-secondary" });

            var driverId = long.Parse(driverIdStr);
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
                return Json(new { statusText = "Inconnu", statusClass = "bg-secondary" });

            var now = DateTime.Now;
            var currentHour = now.Hour;

            // Vérifier si la mission du jour est déjà terminée
            var missionEnded = await _context.DriverMissions_tbl
                .AnyAsync(m => m.Driver_Id == driverId && m.Mission_Date == now.Date && m.Status == "Completed");

            if (missionEnded)
            {
                driver.Driver_Status = "Off Duty";
                await _context.SaveChangesAsync();
                return Json(new { statusText = "🔴 Hors service (mission terminée)", statusClass = "bg-danger" });
            }

            string newStatus;
            string statusText;
            string statusClass;

            if (currentHour >= 7 && currentHour < 8)
            {
                newStatus = "On Route";
                statusText = "🔄 En route";
                statusClass = "bg-warning";
            }
            else if (currentHour >= 8 && currentHour < 17)
            {
                newStatus = "Available";
                statusText = "✅ Disponible";
                statusClass = "bg-success";
            }
            else if (currentHour >= 17)
            {
                newStatus = "On Route";
                statusText = "🔄 En route (retour)";
                statusClass = "bg-warning";
            }
            else
            {
                newStatus = "Off Duty";
                statusText = "🔴 Hors service";
                statusClass = "bg-danger";
            }

            if (driver.Driver_Status != newStatus)
            {
                driver.Driver_Status = newStatus;
                driver.Driver_UpdatedAt = now;
                await _context.SaveChangesAsync();
            }

            return Json(new { statusText = statusText, statusClass = statusClass });
        }

        [HttpPost]
        public async Task<IActionResult> CompleteMission()
        {
            var driverIdStr = HttpContext.Session.GetString("DriverId");
            if (string.IsNullOrEmpty(driverIdStr))
                return Json(new { success = false, message = "Non connecté" });

            var driverId = long.Parse(driverIdStr);
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
                return Json(new { success = false, message = "Chauffeur non trouvé" });

            var now = DateTime.Now;
            var today = now.Date;
            var mission = await _context.DriverMissions_tbl
                .FirstOrDefaultAsync(m => m.Driver_Id == driverId && m.Mission_Date == today);

            if (mission == null)
            {
                mission = new DriverMission
                {
                    Driver_Id = driverId,
                    Bus_Id = driver.Driver_AssignedBusId ?? 0,
                    Mission_Date = today,
                    StartTime = now,
                    EndTime = now,
                    Status = "Completed"
                };
                _context.DriverMissions_tbl.Add(mission);
            }
            else
            {
                mission.EndTime = now;
                mission.Status = "Completed";
            }

            driver.Driver_Status = "Off Duty";
            driver.Driver_UpdatedAt = now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Mission terminée. Bonne soirée !" });
        }
    }
}