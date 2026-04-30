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
    }
}