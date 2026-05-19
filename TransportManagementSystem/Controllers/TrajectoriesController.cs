using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class TrajectoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrajectoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var trajectories = await _context.Trajectories.ToListAsync();
            return View(trajectories);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Trajectory trajectory)
        {
            if (ModelState.IsValid)
            {
                trajectory.Trajectory_CreatedAt = DateTime.Now;
                trajectory.Trajectory_UpdatedAt = DateTime.Now;
                _context.Add(trajectory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(trajectory);
        }

        // API: GET /Trajectories/GetTrajectoryData/{id}
        [HttpGet]
        public async Task<IActionResult> GetTrajectoryData(int id)
        {
            var trajectory = await _context.Trajectories.FindAsync(id);
            if (trajectory == null) return NotFound();

            return Ok(new
            {
                trajectory.Trajectory_Id,
                trajectory.Trajectory_Name,
                trajectory.Trajectory_Code,
                trajectory.Trajectory_Description,
                trajectory.Trajectory_DistanceKm,
                trajectory.Trajectory_EstimatedDurationMinutes,
                trajectory.Trajectory_StartLatitude,
                trajectory.Trajectory_StartLongitude,
                trajectory.Trajectory_EndLatitude,
                trajectory.Trajectory_EndLongitude,
                trajectory.Trajectory_Status
            });
        }

        // API: POST /Trajectories/UpdateTrajectory
        [HttpPost]
        public async Task<IActionResult> UpdateTrajectory([FromBody] TrajectoryUpdateModel model)
        {
            if (model == null || model.Trajectory_Id == 0)
                return BadRequest(new { success = false, message = "Données invalides" });

            var trajectory = await _context.Trajectories.FindAsync(model.Trajectory_Id);
            if (trajectory == null)
                return NotFound(new { success = false, message = "Trajet non trouvé" });

            trajectory.Trajectory_Name = model.Trajectory_Name;
            trajectory.Trajectory_Code = model.Trajectory_Code;
            trajectory.Trajectory_Description = model.Trajectory_Description;
            trajectory.Trajectory_DistanceKm = model.Trajectory_DistanceKm;
            trajectory.Trajectory_EstimatedDurationMinutes = model.Trajectory_EstimatedDurationMinutes;
            trajectory.Trajectory_StartLatitude = model.Trajectory_StartLatitude;
            trajectory.Trajectory_StartLongitude = model.Trajectory_StartLongitude;
            trajectory.Trajectory_EndLatitude = model.Trajectory_EndLatitude;
            trajectory.Trajectory_EndLongitude = model.Trajectory_EndLongitude;
            trajectory.Trajectory_Status = model.Trajectory_Status;
            trajectory.Trajectory_UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            DashboardController.AddNotification("info", "Trajet modifié", $"Le trajet '{trajectory.Trajectory_Name}' a été modifié.");

            return Ok(new { success = true, message = "Trajet mis à jour avec succès" });
        }

        [HttpGet]
        public async Task<IActionResult> GetStopsByTrajectory(int id)
        {
            var trajectory = await _context.Trajectories
                .Where(t => t.Trajectory_Id == id)
                .Select(t => new { t.Trajectory_Name, t.Trajectory_Code })
                .FirstOrDefaultAsync();

            var stops = await _context.TrajectoryStops
                .Where(s => s.TS_TrajectoryId == id)
                .OrderBy(s => s.TS_OrderIndex)
                .Select(s => new
                {
                    s.TS_Id,
                    s.TS_Name,
                    s.TS_Latitude,
                    s.TS_Longitude,
                    s.TS_OrderIndex
                })
                .ToListAsync();

            return Ok(new
            {
                trajectoryId = id,
                trajectoryName = trajectory?.Trajectory_Name ?? "Inconnu",
                trajectoryCode = trajectory?.Trajectory_Code ?? "",
                stopsCount = stops.Count,
                stops = stops
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonnelByTrajectory(int id)
        {
            var personnel = await _context.Personnel
                .Include(p => p.AssignedBus)
                .Where(p => p.AssignedTrajectoryId == id && p.IsAssigned == true)
                .Select(p => new
                {
                    p.Personnel_Id,
                    p.Personnel_FirstName,
                    p.Personnel_LastName,
                    p.Personnel_Gender,
                    p.Personnel_PhoneNumber,
                    p.Personnel_Email,
                    p.Personnel_EmployeeCode,
                    p.Personnel_Department,
                    p.HomeAddress,
                    AssignedBusCode = p.AssignedBus != null ? p.AssignedBus.Bus_Code : "Non assigné",
                    p.Personnel_Status
                })
                .ToListAsync();

            var trajectory = await _context.Trajectories
                .Where(t => t.Trajectory_Id == id)
                .Select(t => new { t.Trajectory_Name, t.Trajectory_Code })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                trajectoryId = id,
                trajectoryName = trajectory?.Trajectory_Name ?? "Inconnu",
                trajectoryCode = trajectory?.Trajectory_Code ?? "",
                personnelCount = personnel.Count,
                personnel = personnel
            });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var trajectory = await _context.Trajectories.FirstOrDefaultAsync(m => m.Trajectory_Id == id);
            if (trajectory == null) return NotFound();
            return View(trajectory);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var trajectory = await _context.Trajectories
                    .FirstOrDefaultAsync(t => t.Trajectory_Id == id);

                if (trajectory == null) return NotFound();

                string trajectoryName = trajectory.Trajectory_Name;

                // 0. Libérer les bus
                var buses = await _context.Buses
                    .Where(b => b.Bus_CurrentTrajectoryId == id)
                    .ToListAsync();
                foreach (var bus in buses)
                    bus.Bus_CurrentTrajectoryId = null;

                // 1. Libérer les personnels assignés à cette trajectoire
                var personnelList = await _context.Personnel
                    .Where(p => p.AssignedTrajectoryId == id)
                    .ToListAsync();
                foreach (var person in personnelList)
                {
                    person.AssignedTrajectoryId = null;
                    person.AssignedBusId = null;
                    person.AssignedStopId = null;
                    person.IsAssigned = false;
                }

                // 2. Supprimer les assignations personnel-trajet
                var personnelAssignments = await _context.PersonnelTrajectoryAssignments
                    .Where(a => a.PTA_TrajectoryId == id)
                    .ToListAsync();
                if (personnelAssignments.Any())
                    _context.PersonnelTrajectoryAssignments.RemoveRange(personnelAssignments);

                // 3. Supprimer les assignations bus-trajet
                var busTrajAssignments = await _context.BusTrajectoryAssignments
                    .Where(a => a.BTA_TrajectoryId == id)
                    .ToListAsync();
                if (busTrajAssignments.Any())
                    _context.BusTrajectoryAssignments.RemoveRange(busTrajAssignments);

                // 4. Supprimer les alertes
                var alerts = await _context.Alerts
                    .Where(a => a.Alert_TrajectoryId == id)
                    .ToListAsync();
                if (alerts.Any())
                    _context.Alerts.RemoveRange(alerts);

                // 5. Supprimer les schedules
                var schedules = await _context.TrajectorySchedules
                    .Where(s => s.TSched_TrajectoryId == id)
                    .ToListAsync();
                if (schedules.Any())
                    _context.TrajectorySchedules.RemoveRange(schedules);

                // 6. Supprimer les driver performances
                var driverPerformances = await _context.DriverPerformance_tbl
                    .Where(p => p.Trajectory_Id == id)
                    .ToListAsync();
                if (driverPerformances.Any())
                    _context.DriverPerformance_tbl.RemoveRange(driverPerformances);

                // 7. Supprimer les stops (points de ramassage) liés à cette trajectoire
                var stops = await _context.TrajectoryStops
                    .Where(s => s.TS_TrajectoryId == id)
                    .ToListAsync();
                if (stops.Any())
                {
                    var stopIds = stops.Select(s => s.TS_Id).ToList();
                    // 🔑 FIX: Libérer les personnels qui référencent ces stops
                    var personnelWithStops = await _context.Personnel
                        .Where(p => stopIds.Contains(p.AssignedStopId ?? 0))
                        .ToListAsync();
                    foreach (var p in personnelWithStops)
                    {
                        p.AssignedStopId = null;
                    }
                    _context.TrajectoryStops.RemoveRange(stops);
                }

                // 8. Supprimer la trajectoire
                _context.Trajectories.Remove(trajectory);
                await _context.SaveChangesAsync();

                DashboardController.AddNotification("delete", "Trajet supprimé", $"Le trajet '{trajectoryName}' a été supprimé.");
                TempData["Success"] = $"✅ Trajectoire '{trajectoryName}' supprimée avec succès!";
            }
            catch (Exception ex)
            {
                var fullMessage = ex.InnerException?.Message ?? ex.Message;
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                TempData["Error"] = $"❌ Erreur lors de la suppression: {fullMessage}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TrajectoryExists(int id) => _context.Trajectories.Any(e => e.Trajectory_Id == id);
    }

    public class TrajectoryUpdateModel
    {
        public int Trajectory_Id { get; set; }
        public string Trajectory_Name { get; set; } = string.Empty;
        public string Trajectory_Code { get; set; } = string.Empty;
        public string? Trajectory_Description { get; set; }
        public decimal? Trajectory_DistanceKm { get; set; }
        public int? Trajectory_EstimatedDurationMinutes { get; set; }
        public decimal? Trajectory_StartLatitude { get; set; }
        public decimal? Trajectory_StartLongitude { get; set; }
        public decimal? Trajectory_EndLatitude { get; set; }
        public decimal? Trajectory_EndLongitude { get; set; }
        public string Trajectory_Status { get; set; } = "Active";
    }
}