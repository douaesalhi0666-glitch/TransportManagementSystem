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

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var trajectory = await _context.Trajectories.FindAsync(id);
            if (trajectory == null) return NotFound();
            return View(trajectory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Trajectory trajectory)
        {
            if (id != trajectory.Trajectory_Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    trajectory.Trajectory_UpdatedAt = DateTime.Now;
                    _context.Update(trajectory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrajectoryExists(trajectory.Trajectory_Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(trajectory);
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
                {
                    _context.PersonnelTrajectoryAssignments.RemoveRange(personnelAssignments);
                }

                // 3. Supprimer les assignations bus-trajet
                var busTrajAssignments = await _context.BusTrajectoryAssignments
                    .Where(a => a.BTA_TrajectoryId == id)
                    .ToListAsync();
                if (busTrajAssignments.Any())
                {
                    _context.BusTrajectoryAssignments.RemoveRange(busTrajAssignments);
                }

                // 4. Supprimer les alertes
                var alerts = await _context.Alerts
                    .Where(a => a.Alert_TrajectoryId == id)
                    .ToListAsync();
                if (alerts.Any())
                {
                    _context.Alerts.RemoveRange(alerts);
                }

                // 5. Supprimer les schedules
                var schedules = await _context.TrajectorySchedules
                    .Where(s => s.TSched_TrajectoryId == id)
                    .ToListAsync();
                if (schedules.Any())
                {
                    _context.TrajectorySchedules.RemoveRange(schedules);
                }

                // 6. Supprimer les driver performances
                var driverPerformances = await _context.DriverPerformance_tbl
                    .Where(p => p.Trajectory_Id == id)
                    .ToListAsync();
                if (driverPerformances.Any())
                {
                    _context.DriverPerformance_tbl.RemoveRange(driverPerformances);
                }

                // 7. Supprimer les stops (points de ramassage)
                var stops = await _context.TrajectoryStops
                    .Where(s => s.TS_TrajectoryId == id)
                    .ToListAsync();
                if (stops.Any())
                {
                    _context.TrajectoryStops.RemoveRange(stops);
                }

                // 8. Supprimer la trajectoire
                _context.Trajectories.Remove(trajectory);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"✅ Trajectoire '{trajectoryName}' supprimée avec succès!";
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                TempData["Error"] = $"❌ Erreur lors de la suppression: {innerMessage}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TrajectoryExists(int id) => _context.Trajectories.Any(e => e.Trajectory_Id == id);
    }
}