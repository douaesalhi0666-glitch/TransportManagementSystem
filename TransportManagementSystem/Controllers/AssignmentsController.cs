using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class AssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AssignmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Correction : utiliser Driver_AssignedBus au lieu de AssignedBus
            var drivers = await _context.Drivers
                .Include(d => d.Driver_AssignedBus)  // ← corrigé
                .ToListAsync();

            // Pour les bus, on supprime les Include qui n'existent pas
            // Si vous avez des propriétés de navigation comme Bus_CurrentDriver, Bus_CurrentTrajectory, vous pouvez les ajouter
            var buses = await _context.Buses
                .ToListAsync();

            var trajectories = await _context.Trajectories.ToListAsync();

            ViewBag.Drivers = drivers;
            ViewBag.Buses = buses;
            ViewBag.Trajectories = trajectories;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignDriverToBus(long driverId, long busId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            var bus = await _context.Buses.FindAsync(busId);

            if (driver == null || bus == null)
            {
                TempData["Error"] = "Chauffeur ou bus non trouvé.";
                return RedirectToAction("Index");
            }

            driver.Driver_AssignedBusId = busId;
            bus.Bus_CurrentDriverId = driverId; // Assurez-vous que cette colonne existe
            await _context.SaveChangesAsync();

            TempData["Success"] = "Chauffeur assigné au bus avec succès.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AssignBusToTrajectory(long busId, int trajectoryId)
        {
            var bus = await _context.Buses.FindAsync(busId);

            if (bus == null)
            {
                TempData["Error"] = "Bus non trouvé.";
                return RedirectToAction("Index");
            }

            bus.Bus_CurrentTrajectoryId = trajectoryId; // Assurez-vous que cette colonne existe
            await _context.SaveChangesAsync();

            TempData["Success"] = "Bus assigné au trajet avec succès.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UnassignDriver(long driverId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);

            if (driver != null && driver.Driver_AssignedBusId != null)
            {
                var bus = await _context.Buses.FindAsync(driver.Driver_AssignedBusId.Value);
                if (bus != null)
                {
                    bus.Bus_CurrentDriverId = null;
                }
                driver.Driver_AssignedBusId = null;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Chauffeur désassigné.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UnassignBus(long busId)
        {
            var bus = await _context.Buses.FindAsync(busId);

            if (bus != null)
            {
                bus.Bus_CurrentTrajectoryId = null;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Trajet désassigné.";
            }

            return RedirectToAction("Index");
        }
    }
}