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

        // GET: Drivers
        public async Task<IActionResult> Index()
        {
            var drivers = await _context.Drivers
                .Include(d => d.AssignedBus)
                    .ThenInclude(b => b.AssignedFragment)
                .ToListAsync();
            return View(drivers);
        }

        // GET: Drivers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Drivers/Create
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

        // GET: Drivers/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers
                .Include(d => d.AssignedBus)
                    .ThenInclude(b => b.AssignedFragment)
                .FirstOrDefaultAsync(d => d.Driver_id == id);

            if (driver == null)
            {
                return NotFound();
            }

            if (driver.Driver_AssignedBusId != null)
            {
                ViewBag.Warning = $"⚠️ Ce chauffeur est actuellement assigné au bus {driver.AssignedBus?.Bus_Code}. " +
                                  "Si vous le mettez hors service, ce bus n'aura plus de chauffeur.";
            }

            return View(driver);
        }

        // POST: Drivers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Driver driver)
        {
            if (id != driver.Driver_id)
            {
                return NotFound();
            }

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
                    if (!DriverExists(driver.Driver_id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(driver);
        }

        // GET: Drivers/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers
                .Include(d => d.AssignedBus)
                    .ThenInclude(b => b.AssignedFragment)
                .FirstOrDefaultAsync(m => m.Driver_id == id);
            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        // POST: Drivers/Delete/5
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
                    if (bus != null)
                    {
                        bus.Bus_CurrentDriverId = null;
                    }
                }
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DriverExists(long id)
        {
            return _context.Drivers.Any(e => e.Driver_id == id);
        }

        // ========== AUTO STATUS MANAGEMENT ==========

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

            return Json(new { success = true, message = "Mission terminée. Bonne soirée !" });
        }

        private async Task<string> CalculateDriverStatus(long driverId)
        {
            var now = DateTime.Now;
            var currentHour = now.Hour;

            var mission = await _context.DriverMissions_tbl
                .Where(m => m.Driver_Id == driverId && m.Mission_Date == now.Date && m.Status == "Completed")
                .FirstOrDefaultAsync();

            if (mission != null)
            {
                return "Off Duty";
            }

            if (currentHour >= 7 && currentHour < 8)
            {
                return "On Route";
            }
            else if (currentHour >= 8 && currentHour < 17)
            {
                return "Available";
            }
            else if (currentHour >= 17)
            {
                return "On Route";
            }
            else
            {
                return "Off Duty";
            }
        }
    }
}