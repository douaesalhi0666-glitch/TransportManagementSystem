using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;
using TransportManagementSystem.ViewModels;

namespace TransportManagementSystem.Controllers
{
    public class TrajectoryAssignmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrajectoryAssignmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ========== ASSIGNATION PERSONNEL (Trajectoires) ==========
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

        // ========== OCCUPATION DES BUS ==========
        public async Task<IActionResult> BusOccupancy()
        {
            var buses = await _context.Buses
                .Include(b => b.CurrentDriver)
                .Include(b => b.CurrentTrajectory)
                .Where(b => b.Bus_CurrentTrajectoryId != null && b.Bus_CurrentTrajectoryId != 1)
                .ToListAsync();

            var busOccupancyList = new List<BusOccupancyViewModel>();

            foreach (var bus in buses)
            {
                var personnel = await _context.Personnel
                    .Where(p => p.AssignedBusId == bus.Bus_Id && p.IsAssigned == true)
                    .ToListAsync();

                busOccupancyList.Add(new BusOccupancyViewModel
                {
                    Bus = bus,
                    Occupancy = personnel.Count,
                    Capacity = bus.Bus_Capacity ?? 50,
                    Personnel = personnel
                });
            }

            return View(busOccupancyList);
        }

        // ========== ASSIGNER PERSONNEL À UN BUS (AUTO + MANUEL) ==========
        [HttpPost]
        public async Task<IActionResult> AssignPersonnelToBus(long busId, int? personnelId = null)
        {
            var bus = await _context.Buses
                .Include(b => b.CurrentTrajectory)
                .FirstOrDefaultAsync(b => b.Bus_Id == busId);

            if (bus == null)
            {
                TempData["Error"] = "Bus non trouvé.";
                return RedirectToAction("BusOccupancy");
            }

            if (bus.Bus_CurrentTrajectoryId == null || bus.Bus_CurrentTrajectoryId == 1)
            {
                TempData["Error"] = "Ce bus n'a pas de trajectoire valide.";
                return RedirectToAction("BusOccupancy");
            }

            var trajectoryId = bus.Bus_CurrentTrajectoryId.Value;
            int capacity = bus.Bus_Capacity ?? 20;
            int currentOccupancy = await _context.Personnel.CountAsync(p => p.AssignedBusId == busId);

            // Manual assignment for a specific personnel
            if (personnelId.HasValue && personnelId.Value > 0)
            {
                var worker = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.Personnel_Id == personnelId.Value
                        && p.AssignedTrajectoryId == trajectoryId
                        && p.IsMotorized == false
                        && p.Personnel_Status == "Active"
                        && (p.AssignedBusId == null || p.AssignedBusId == 0));

                if (worker == null)
                {
                    TempData["Error"] = "Ce personnel n'est pas disponible ou déjà assigné.";
                    return RedirectToAction("BusOccupancy");
                }

                if (currentOccupancy >= capacity)
                {
                    TempData["Error"] = $"Le bus est déjà plein (capacité: {capacity}).";
                    return RedirectToAction("BusOccupancy");
                }

                worker.AssignedBusId = busId;
                worker.IsAssigned = true;
                currentOccupancy++;
                bus.CurrentOccupancy = currentOccupancy;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"✅ Personnel {worker.Personnel_FirstName} {worker.Personnel_LastName} assigné au bus.";
                return RedirectToAction("BusOccupancy");
            }

            // Auto-assign: find all unassigned personnel in this trajectory
            var unassignedWorkers = await _context.Personnel
                .Where(p => p.AssignedTrajectoryId == trajectoryId
                            && p.IsMotorized == false
                            && p.Personnel_Status == "Active"
                            && (p.AssignedBusId == null || p.AssignedBusId == 0))
                .OrderBy(p => p.AssignedStopId)
                .ToListAsync();

            if (!unassignedWorkers.Any())
            {
                TempData["Error"] = "Aucun personnel non assigné trouvé pour cette trajectoire.";
                return RedirectToAction("BusOccupancy");
            }

            int assignedCount = 0;
            foreach (var worker in unassignedWorkers)
            {
                if (currentOccupancy + assignedCount >= capacity)
                    break;

                worker.AssignedBusId = busId;
                worker.IsAssigned = true;
                assignedCount++;
            }

            bus.CurrentOccupancy = currentOccupancy + assignedCount;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"✅ {assignedCount} personnels assignés au bus (Trajectoire: {bus.CurrentTrajectory?.Trajectory_Name}, Capacité: {capacity}, Occupation: {currentOccupancy + assignedCount})";
            return RedirectToAction("BusOccupancy");
        }

        // ========== DÉSASSIGNER PERSONNEL D'UN BUS ==========
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
                personnel.IsAssigned = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Personnel {personnel.Personnel_FirstName} {personnel.Personnel_LastName} retiré du bus.";
            }
            else
            {
                TempData["Error"] = "Ce personnel n'est pas assigné à ce bus.";
            }

            return RedirectToAction("BusOccupancy");
        }

        // ========== OBTENIR LES PERSONNELS NON ASSIGNÉS POUR UN BUS ==========
        [HttpGet]
        public async Task<IActionResult> GetUnassignedPersonnelForBus(long busId)
        {
            var bus = await _context.Buses
                .FirstOrDefaultAsync(b => b.Bus_Id == busId);

            if (bus == null)
                return NotFound();

            var trajectoryId = bus.Bus_CurrentTrajectoryId;
            if (trajectoryId == null || trajectoryId == 1)
                return Ok(new List<object>());

            var unassignedWorkers = await _context.Personnel
                .Where(p => p.AssignedTrajectoryId == trajectoryId
                            && p.IsMotorized == false
                            && p.Personnel_Status == "Active"
                            && (p.AssignedBusId == null || p.AssignedBusId == 0))
                .Select(p => new
                {
                    p.Personnel_Id,
                    p.Personnel_FirstName,
                    p.Personnel_LastName,
                    p.AssignedStopId,
                    StopName = _context.TrajectoryStops
                        .Where(s => s.TS_Id == p.AssignedStopId)
                        .Select(s => s.TS_Name)
                        .FirstOrDefault() ?? "N/A"
                })
                .ToListAsync();

            return Ok(unassignedWorkers);
        }

        // ========== SYNCHRONISATION ==========
        [HttpPost]
        public async Task<IActionResult> SyncPersonnelBusAssignments()
        {
            var personnelList = await _context.Personnel
                .Where(p => p.AssignedStopId != null && p.AssignedBusId == null && p.IsAssigned == true)
                .ToListAsync();

            int updatedCount = 0;
            foreach (var p in personnelList)
            {
                var stop = await _context.TrajectoryStops.FindAsync(p.AssignedStopId);
                if (stop == null) continue;

                var trajectory = await _context.Trajectories.FindAsync(stop.TS_TrajectoryId);
                if (trajectory == null) continue;

                var bus = await _context.Buses
                    .FirstOrDefaultAsync(b => b.Bus_CurrentTrajectoryId == trajectory.Trajectory_Id
                                           && b.Bus_Status == "In Service");
                if (bus != null)
                {
                    p.AssignedBusId = bus.Bus_Id;
                    updatedCount++;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Synchronisation terminée : {updatedCount} personnels assignés à un bus.";
            return RedirectToAction("BusOccupancy");
        }

        // ========== GESTION BUS-TRAJECTOIRE ==========
        [HttpGet]
        public async Task<IActionResult> GetAvailableBuses()
        {
            var buses = await _context.Buses
                .Where(b => b.Bus_Status == "In Service" && b.Bus_CurrentTrajectoryId == null)
                .Select(b => new { b.Bus_Id, b.Bus_Code, b.Bus_PlateNumber })
                .ToListAsync();
            return Ok(buses);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableTrajectories()
        {
            var trajectories = await _context.Trajectories
                .Where(t => t.Trajectory_Status == "Active"
                            && t.Trajectory_Id > 1
                            && !_context.Buses.Any(b => b.Bus_CurrentTrajectoryId == t.Trajectory_Id))
                .Select(t => new { t.Trajectory_Id, t.Trajectory_Name, t.Trajectory_Code })
                .ToListAsync();
            return Ok(trajectories);
        }

        [HttpPost]
        public async Task<IActionResult> AssignBusToTrajectory(long busId, int trajectoryId)
        {
            var bus = await _context.Buses.FindAsync(busId);
            if (bus == null)
                return Json(new { success = false, message = "Bus non trouvé." });

            var trajectory = await _context.Trajectories.FindAsync(trajectoryId);
            if (trajectory == null)
                return Json(new { success = false, message = "Trajectoire non trouvée." });

            if (bus.Bus_CurrentTrajectoryId != null)
                return Json(new { success = false, message = "Ce bus est déjà assigné à une trajectoire." });

            bus.Bus_CurrentTrajectoryId = trajectoryId;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Bus {bus.Bus_Code} assigné à la trajectoire {trajectory.Trajectory_Name}." });
        }

        [HttpPost]
        public async Task<IActionResult> UnassignBus(long busId)
        {
            var bus = await _context.Buses.FindAsync(busId);
            if (bus == null)
                return Json(new { success = false, message = "Bus non trouvé." });

            if (bus.Bus_CurrentTrajectoryId == null)
                return Json(new { success = false, message = "Ce bus n'est assigné à aucune trajectoire." });

            bus.Bus_CurrentTrajectoryId = null;
            bus.CurrentOccupancy = 0;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Bus {bus.Bus_Code} désassigné avec succès." });
        }

        // ========== HELPERS ==========
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
}