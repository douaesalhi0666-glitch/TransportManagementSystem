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

        public async Task<IActionResult> AssignPersonnel()
        {
            ViewBag.Personnel = await _context.Personnel
                .Where(p => p.IsAssigned != true)
                .ToListAsync();

            ViewBag.Trajectories = await _context.Trajectories
                .Where(t => t.Trajectory_Status == "Active")
                .ToListAsync();

            var assignments = await _context.Personnel
                .Include(p => p.AssignedTrajectory)
                .Where(p => p.IsAssigned == true)
                .ToListAsync();

            return View(assignments);
        }

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

            if (personnel.Personnel_Latitude == null || personnel.Personnel_Longitude == null)
            {
                TempData["Error"] = "Les coordonnées GPS de ce personnel ne sont pas définies.";
                return RedirectToAction("AssignPersonnel");
            }

            Trajectory? bestTrajectory = null;
            double bestDistance = double.MaxValue;

            foreach (var traj in trajectories)
            {
                if (traj.Trajectory_EndLatitude != null && traj.Trajectory_EndLongitude != null)
                {
                    double distance = CalculateDistance(
                        (double)personnel.Personnel_Latitude,
                        (double)personnel.Personnel_Longitude,
                        (double)traj.Trajectory_EndLatitude,
                        (double)traj.Trajectory_EndLongitude
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

            personnel.AssignedTrajectoryId = bestTrajectory.Trajectory_Id;
            personnel.IsAssigned = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"🤖 {personnel.Personnel_FirstName} {personnel.Personnel_LastName} assigné au trajet {bestTrajectory.Trajectory_Name} (distance: {bestDistance:F1} km).";
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

            personnel.AssignedTrajectoryId = trajectoryId;
            personnel.IsAssigned = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{personnel.Personnel_FirstName} {personnel.Personnel_LastName} assigné au trajet {trajectory.Trajectory_Name}.";
            return RedirectToAction("AssignPersonnel");
        }

        [HttpPost]
        public async Task<IActionResult> UnassignPersonnel(long personnelId)
        {
            var personnel = await _context.Personnel.FindAsync(personnelId);
            if (personnel != null)
            {
                personnel.AssignedTrajectoryId = null;
                personnel.AssignedBusId = null;
                personnel.AssignedStopId = null;
                personnel.IsAssigned = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Personnel désassigné avec succès.";
            }
            return RedirectToAction("AssignPersonnel");
        }

        public async Task<IActionResult> ViewAssignments()
        {
            var assignments = await _context.Personnel
                .Include(p => p.AssignedTrajectory)
                .Include(p => p.AssignedBus)
                .Include(p => p.AssignedStop)
                .Where(p => p.IsAssigned == true)
                .ToListAsync();
            return View(assignments);
        }

        // ========== BUS OCCUPANCY CORRIGÉE ==========
        public async Task<IActionResult> BusOccupancy()
        {
            // Exclure les bus dont la trajectoire est NULL ou == 1
            var buses = await _context.Buses
                .Include(b => b.CurrentDriver)
                .Include(b => b.CurrentTrajectory)
                .Where(b => b.Bus_CurrentTrajectoryId != null && b.Bus_CurrentTrajectoryId != 1)
                .ToListAsync();

            var busOccupancy = new List<BusOccupancyViewModel>();

            foreach (var bus in buses)
            {
                var personnel = await _context.Personnel
                    .Where(p => p.AssignedTrajectoryId == bus.Bus_CurrentTrajectoryId
                                && p.IsAssigned == true)
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

        // ========== ASSIGNER PERSONNEL AU BUS ==========
        [HttpPost]
        public async Task<IActionResult> AssignPersonnelToBus(long busId)
        {
            var bus = await _context.Buses.FindAsync(busId);
            if (bus == null)
            {
                TempData["Error"] = "Bus non trouvé.";
                return RedirectToAction("BusOccupancy");
            }

            var personnelToAssign = await _context.Personnel
                .Where(p => p.IsAssigned == false
                            && p.AssignedStopId != null
                            && p.AssignedBusId == null
                            && p.Personnel_Status == "Active")
                .ToListAsync();

            var capacity = bus.Bus_Capacity ?? 50;
            var currentOccupancy = await _context.Personnel.CountAsync(p => p.AssignedBusId == busId);

            int assignedCount = 0;
            foreach (var person in personnelToAssign)
            {
                if (currentOccupancy + assignedCount >= capacity)
                    break;

                person.AssignedBusId = busId;
                person.IsAssigned = true;
                assignedCount++;
            }

            bus.CurrentOccupancy = currentOccupancy + assignedCount;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{assignedCount} personnels assignés au bus (Capacité: {capacity}, Occupation: {currentOccupancy + assignedCount})";
            return RedirectToAction("BusOccupancy");
        }

        // ========== DÉSASSIGNER PERSONNEL D'UN BUS (CORRIGÉE) ==========
        [HttpPost]
        public async Task<IActionResult> RemovePersonnelFromBus(long personnelId, long busId)
        {
            var personnel = await _context.Personnel.FindAsync(personnelId);
            if (personnel == null)
            {
                TempData["Error"] = "Personnel non trouvé.";
                return RedirectToAction("BusOccupancy");
            }

            if (personnel.AssignedBusId == busId)
            {
                personnel.AssignedBusId = null;
                personnel.IsAssigned = false;  // Important : le personnel n'est plus assigné à ce bus
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Personnel {personnel.Personnel_FirstName} {personnel.Personnel_LastName} retiré du bus.";
            }
            else
            {
                TempData["Error"] = "Ce personnel n'est pas assigné à ce bus.";
            }

            return RedirectToAction("BusOccupancy");
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

        private double ToRadians(double degrees) => degrees * Math.PI / 180;
    }

    public class BusOccupancyViewModel
    {
        public Bus Bus { get; set; } = null!;
        public int Occupancy { get; set; }
        public int Capacity { get; set; }
        public List<Personnel> Personnel { get; set; } = new List<Personnel>();
    }
}