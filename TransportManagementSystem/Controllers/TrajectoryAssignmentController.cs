using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class TrajectoryAssignmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrajectoryAssignmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Assign Personnel page
        public async Task<IActionResult> AssignPersonnel()
        {
            ViewBag.Personnel = await _context.Personnel
                .Where(p => p.IsAssigned != true)
                .ToListAsync();

            ViewBag.Trajectories = await _context.Trajectories.ToListAsync();

            return View();
        }

        // POST: AI-based assignment
        [HttpPost]
        public async Task<IActionResult> AssignPersonnelAI(long personnelId)
        {
            var personnel = await _context.Personnel.FindAsync(personnelId);

            if (personnel == null)
            {
                TempData["Error"] = "Personnel non trouvé.";
                return RedirectToAction("AssignPersonnel");
            }

            var trajectories = await _context.Trajectories
                .Where(t => t.Trajectory_Status == "Active")
                .ToListAsync();

            if (!trajectories.Any())
            {
                TempData["Error"] = "Aucun trajet actif disponible.";
                return RedirectToAction("AssignPersonnel");
            }

            Trajectory? bestTrajectory = null;
            double bestDistance = double.MaxValue;

            foreach (var traj in trajectories)
            {
                if (personnel.Personnel_Latitude != null && personnel.Personnel_Longitude != null &&
                    traj.Trajectory_StartLatitude != null && traj.Trajectory_StartLongitude != null)
                {
                    double distance = CalculateDistance(
                        (double)personnel.Personnel_Latitude,
                        (double)personnel.Personnel_Longitude,
                        (double)traj.Trajectory_StartLatitude,
                        (double)traj.Trajectory_StartLongitude
                    );

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestTrajectory = traj;
                    }
                }
            }

            if (bestTrajectory == null)
            {
                TempData["Error"] = "Impossible de trouver un trajet proche.";
                return RedirectToAction("AssignPersonnel");
            }

            var availableBuses = await _context.Buses
                .Where(b => b.Bus_Status == "In Service" && b.Bus_CurrentTrajectoryId == bestTrajectory.Trajectory_Id)
                .OrderBy(b => b.Bus_Id)
                .ToListAsync();

            if (!availableBuses.Any())
            {
                TempData["Error"] = $"Aucun bus disponible sur le trajet {bestTrajectory.Trajectory_Name}.";
                return RedirectToAction("AssignPersonnel");
            }

            Bus? selectedBus = null;
            foreach (var bus in availableBuses)
            {
                int currentOccupancy = await _context.Personnel
                    .CountAsync(p => p.AssignedBusId == bus.Bus_Id);

                int capacity = bus.Bus_Capacity ?? 50;

                if (currentOccupancy < capacity)
                {
                    selectedBus = bus;
                    break;
                }
            }

            if (selectedBus == null)
            {
                TempData["Error"] = $"Tous les bus du trajet {bestTrajectory.Trajectory_Name} sont pleins.";
                return RedirectToAction("AssignPersonnel");
            }

            personnel.AssignedTrajectoryId = bestTrajectory.Trajectory_Id;
            personnel.AssignedBusId = selectedBus.Bus_Id;
            personnel.IsAssigned = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"🤖 {personnel.Personnel_FirstName} {personnel.Personnel_LastName} assigné au trajet {bestTrajectory.Trajectory_Name} (distance: {bestDistance:F1} km) et au bus {selectedBus.Bus_Code}.";

            return RedirectToAction("AssignPersonnel");
        }

        [HttpPost]
        public async Task<IActionResult> AssignPersonnelManual(long personnelId, int trajectoryId)
        {
            var personnel = await _context.Personnel.FindAsync(personnelId);
            var trajectory = await _context.Trajectories.FindAsync(trajectoryId);

            if (personnel == null || trajectory == null)
            {
                TempData["Error"] = "Personnel ou trajet non trouvé.";
                return RedirectToAction("AssignPersonnel");
            }

            var availableBuses = await _context.Buses
                .Where(b => b.Bus_CurrentTrajectoryId == trajectoryId && b.Bus_Status == "In Service")
                .OrderBy(b => b.Bus_Id)
                .ToListAsync();

            if (!availableBuses.Any())
            {
                TempData["Error"] = "Aucun bus disponible sur ce trajet.";
                return RedirectToAction("AssignPersonnel");
            }

            Bus? selectedBus = null;
            foreach (var bus in availableBuses)
            {
                int currentOccupancy = await _context.Personnel
                    .CountAsync(p => p.AssignedBusId == bus.Bus_Id);

                int capacity = bus.Bus_Capacity ?? 50;

                if (currentOccupancy < capacity)
                {
                    selectedBus = bus;
                    break;
                }
            }

            if (selectedBus == null)
            {
                TempData["Error"] = "Tous les bus de ce trajet sont pleins.";
                return RedirectToAction("AssignPersonnel");
            }

            personnel.AssignedTrajectoryId = trajectoryId;
            personnel.AssignedBusId = selectedBus.Bus_Id;
            personnel.IsAssigned = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{personnel.Personnel_FirstName} {personnel.Personnel_LastName} assigné au trajet {trajectory.Trajectory_Name} et au bus {selectedBus.Bus_Code}.";

            return RedirectToAction("AssignPersonnel");
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public async Task<IActionResult> ViewAssignments()
        {
            var assignments = await _context.Personnel
                .Include(p => p.AssignedTrajectory)
                .Include(p => p.AssignedBus)
                .Where(p => p.IsAssigned == true)
                .ToListAsync();

            return View(assignments);
        }

        [HttpPost]
        public async Task<IActionResult> UnassignPersonnel(long personnelId)
        {
            var personnel = await _context.Personnel.FindAsync(personnelId);

            if (personnel != null)
            {
                personnel.AssignedTrajectoryId = null;
                personnel.AssignedBusId = null;
                personnel.IsAssigned = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Personnel désassigné avec succès.";
            }

            return RedirectToAction("ViewAssignments");
        }

        public async Task<IActionResult> BusOccupancy()
        {
            var buses = await _context.Buses
                .Include(b => b.CurrentDriver)
                .Include(b => b.CurrentTrajectory)
                .ToListAsync();

            var busOccupancy = new List<BusOccupancyViewModel>();

            foreach (var bus in buses)
            {
                var personnel = await _context.Personnel
                    .Include(p => p.AssignedTrajectory)
                    .Where(p => p.AssignedBusId == bus.Bus_Id && p.IsAssigned == true)
                    .ToListAsync();

                busOccupancy.Add(new BusOccupancyViewModel
                {
                    Bus = bus,
                    Occupancy = personnel.Count,
                    Capacity = bus.Bus_Capacity ?? 50,
                    Personnel = personnel
                });
            }

            ViewBag.BusOccupancy = busOccupancy;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RemovePersonnelFromBus(long personnelId, long busId)
        {
            var personnel = await _context.Personnel.FindAsync(personnelId);

            if (personnel != null && personnel.AssignedBusId == busId)
            {
                personnel.AssignedTrajectoryId = null;
                personnel.AssignedBusId = null;
                personnel.IsAssigned = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Personnel retiré du bus avec succès.";
            }

            return RedirectToAction("BusOccupancy");
        }
    }

    public class BusOccupancyViewModel
    {
        public Bus Bus { get; set; } = null!;
        public int Occupancy { get; set; }
        public int Capacity { get; set; }
        public List<Personnel> Personnel { get; set; } = new List<Personnel>();
    }
}