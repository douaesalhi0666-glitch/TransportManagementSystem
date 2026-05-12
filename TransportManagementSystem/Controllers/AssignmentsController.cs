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

        // GET: Assignments
        public async Task<IActionResult> Index()
        {
            var drivers = await _context.Drivers
                .Where(d => d.Driver_Status == "Available")
                .Include(d => d.AssignedBus)
                .ToListAsync();

            var buses = await _context.Buses
                .Where(b => b.Bus_Status == "In Service")
                .Include(b => b.CurrentDriver)
                .ToListAsync();

            ViewBag.Drivers = drivers;
            ViewBag.Buses = buses;
            return View();
        }

        // POST: Assign Driver to Bus
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

            if (driver.Driver_Status != "Available")
            {
                TempData["Error"] = "Ce chauffeur n'est pas disponible. Statut actuel: " + driver.Driver_Status;
                return RedirectToAction("Index");
            }

            if (bus.Bus_Status != "In Service")
            {
                TempData["Error"] = "Ce bus n'est pas en service. Statut actuel: " + bus.Bus_Status;
                return RedirectToAction("Index");
            }

            if (bus.Bus_CurrentDriverId != null)
            {
                TempData["Error"] = "Ce bus a déjà un chauffeur. Désassignez-le d'abord.";
                return RedirectToAction("Index");
            }

            if (driver.Driver_AssignedBusId != null)
            {
                TempData["Error"] = "Ce chauffeur a déjà un bus. Désassignez-le d'abord.";
                return RedirectToAction("Index");
            }

            driver.Driver_AssignedBusId = busId;
            bus.Bus_CurrentDriverId = driverId;
            driver.Driver_Status = "On Route";

            await _context.SaveChangesAsync();

            DashboardController.AddNotification("assignment", "Chauffeur assigné", $"Le chauffeur {driver.Driver_FirstName} {driver.Driver_LastName} a été assigné au bus {bus.Bus_Code}.");

            TempData["Success"] = $"Chauffeur {driver.Driver_FirstName} {driver.Driver_LastName} assigné au bus {bus.Bus_Code}.";
            return RedirectToAction("Index");
        }

        // POST: Unassign Driver
        [HttpPost]
        public async Task<IActionResult> UnassignDriver(long driverId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            string driverName = $"{driver?.Driver_FirstName} {driver?.Driver_LastName}";

            if (driver != null && driver.Driver_AssignedBusId != null)
            {
                var bus = await _context.Buses.FindAsync(driver.Driver_AssignedBusId.Value);
                if (bus != null)
                {
                    bus.Bus_CurrentDriverId = null;
                }
                driver.Driver_AssignedBusId = null;
                driver.Driver_Status = "Available";

                await _context.SaveChangesAsync();

                DashboardController.AddNotification("unassignment", "Chauffeur désassigné", $"Le chauffeur {driverName} a été désassigné de son bus.");

                TempData["Success"] = "Chauffeur désassigné avec succès.";
            }

            return RedirectToAction("Index");
        }
    }
}