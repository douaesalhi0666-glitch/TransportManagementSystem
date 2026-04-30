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
            // Only show drivers with status "Available"
            var drivers = await _context.Drivers
                .Where(d => d.Driver_Status == "Available")
                .Include(d => d.AssignedBus)
                .ToListAsync();

            // Only show buses with status "In Service"
            var buses = await _context.Buses
                .Where(b => b.Bus_Status == "In Service")
                .Include(b => b.CurrentDriver)
                .Include(b => b.CurrentTrajectory)
                .ToListAsync();

            var trajectories = await _context.Trajectories.ToListAsync();

            ViewBag.Drivers = drivers;
            ViewBag.Buses = buses;
            ViewBag.Trajectories = trajectories;

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

            // Check if driver is available
            if (driver.Driver_Status != "Available")
            {
                TempData["Error"] = "Ce chauffeur n'est pas disponible. Statut actuel: " + driver.Driver_Status;
                return RedirectToAction("Index");
            }

            // Check if bus is in service
            if (bus.Bus_Status != "In Service")
            {
                TempData["Error"] = "Ce bus n'est pas en service. Statut actuel: " + bus.Bus_Status;
                return RedirectToAction("Index");
            }

            // Check if bus already has a driver
            if (bus.Bus_CurrentDriverId != null)
            {
                TempData["Error"] = "Ce bus a déjà un chauffeur. Désassignez-le d'abord.";
                return RedirectToAction("Index");
            }

            // Check if driver already has a bus
            if (driver.Driver_AssignedBusId != null)
            {
                TempData["Error"] = "Ce chauffeur a déjà un bus. Désassignez-le d'abord.";
                return RedirectToAction("Index");
            }

            // Assign driver to bus
            driver.Driver_AssignedBusId = busId;
            bus.Bus_CurrentDriverId = driverId;

            // Update driver status to "On Route" when assigned
            driver.Driver_Status = "On Route";

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Chauffeur {driver.Driver_FirstName} {driver.Driver_LastName} assigné au bus {bus.Bus_Code}.";
            return RedirectToAction("Index");
        }

        // POST: Assign Bus to Trajectory
        [HttpPost]
        public async Task<IActionResult> AssignBusToTrajectory(long busId, int trajectoryId)
        {
            var bus = await _context.Buses.FindAsync(busId);
            var trajectory = await _context.Trajectories.FindAsync(trajectoryId);

            if (bus == null || trajectory == null)
            {
                TempData["Error"] = "Bus ou trajet non trouvé.";
                return RedirectToAction("Index");
            }

            bus.Bus_CurrentTrajectoryId = trajectoryId;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Bus {bus.Bus_Code} assigné au trajet {trajectory.Trajectory_Name}.";
            return RedirectToAction("Index");
        }

        // POST: Unassign Driver
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

                // Reset driver status to "Available" when unassigned
                driver.Driver_Status = "Available";

                await _context.SaveChangesAsync();
                TempData["Success"] = "Chauffeur désassigné avec succès.";
            }

            return RedirectToAction("Index");
        }

        // POST: Unassign Bus from Trajectory
        [HttpPost]
        public async Task<IActionResult> UnassignBus(long busId)
        {
            var bus = await _context.Buses.FindAsync(busId);

            if (bus != null)
            {
                bus.Bus_CurrentTrajectoryId = null;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Trajet désassigné avec succès.";
            }

            return RedirectToAction("Index");
        }
    }
}