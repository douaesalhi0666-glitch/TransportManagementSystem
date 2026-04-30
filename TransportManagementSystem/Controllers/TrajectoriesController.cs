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

        // GET: Trajectories
        public async Task<IActionResult> Index()
        {
            var trajectories = await _context.Trajectories.ToListAsync();
            return View(trajectories);
        }

        // GET: Trajectories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Trajectories/Create
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

        // GET: Trajectories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trajectory = await _context.Trajectories.FindAsync(id);
            if (trajectory == null)
            {
                return NotFound();
            }
            return View(trajectory);
        }

        // POST: Trajectories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Trajectory trajectory)
        {
            if (id != trajectory.Trajectory_Id)
            {
                return NotFound();
            }

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
                    if (!TrajectoryExists(trajectory.Trajectory_Id))
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
            return View(trajectory);
        }

        // GET: Trajectories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trajectory = await _context.Trajectories
                .FirstOrDefaultAsync(m => m.Trajectory_Id == id);
            if (trajectory == null)
            {
                return NotFound();
            }

            return View(trajectory);
        }

        // POST: Trajectories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // 1. Remove trajectory reference from all buses
            var busesWithThisTrajectory = await _context.Buses
                .Where(b => b.Bus_CurrentTrajectoryId == id)
                .ToListAsync();

            foreach (var bus in busesWithThisTrajectory)
            {
                bus.Bus_CurrentTrajectoryId = null;
            }

            // 2. Delete BusTrajectoryAssignment records
            var busAssignments = await _context.BusTrajectoryAssignments
                .Where(a => a.BTA_TrajectoryId == id)
                .ToListAsync();

            if (busAssignments.Any())
            {
                _context.BusTrajectoryAssignments.RemoveRange(busAssignments);
            }

            // 3. Delete PersonnelTrajectoryAssignment records
            var personnelAssignments = await _context.PersonnelTrajectoryAssignments
                .Where(a => a.PTA_TrajectoryId == id)
                .ToListAsync();

            if (personnelAssignments.Any())
            {
                _context.PersonnelTrajectoryAssignments.RemoveRange(personnelAssignments);
            }

            // 4. Delete Alert records
            var alerts = await _context.Alerts
                .Where(a => a.Alert_TrajectoryId == id)
                .ToListAsync();

            if (alerts.Any())
            {
                _context.Alerts.RemoveRange(alerts);
            }

            // 5. Delete TrajectorySchedule records
            var schedules = await _context.TrajectorySchedules
                .Where(s => s.TSched_TrajectoryId == id)
                .ToListAsync();

            if (schedules.Any())
            {
                _context.TrajectorySchedules.RemoveRange(schedules);
            }

            // 6. Unassign all personnel
            var personnelWithThisTrajectory = await _context.Personnel
                .Where(p => p.AssignedTrajectoryId == id)
                .ToListAsync();

            foreach (var person in personnelWithThisTrajectory)
            {
                person.AssignedTrajectoryId = null;
                person.AssignedBusId = null;
                person.IsAssigned = false;
            }

            // 7. Delete DriverPerformance records
            var driverPerformances = await _context.DriverPerformance_tbl
                .Where(p => p.Trajectory_Id == id)
                .ToListAsync();

            if (driverPerformances.Any())
            {
                _context.DriverPerformance_tbl.RemoveRange(driverPerformances);
            }

            // 8. Delete RecommendationLog records
            var recommendationLogs = await _context.RecommendationLogs
                .Where(r => r.Recommended_TrajectoryId == id)
                .ToListAsync();

            if (recommendationLogs.Any())
            {
                _context.RecommendationLogs.RemoveRange(recommendationLogs);
            }

            // 9. Delete all stops
            var stops = await _context.TrajectoryStops
                .Where(s => s.TS_TrajectoryId == id)
                .ToListAsync();

            if (stops.Any())
            {
                _context.TrajectoryStops.RemoveRange(stops);
            }

            // 10. Finally delete the trajectory
            var trajectory = await _context.Trajectories.FindAsync(id);
            if (trajectory != null)
            {
                _context.Trajectories.Remove(trajectory);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool TrajectoryExists(int id)
        {
            return _context.Trajectories.Any(e => e.Trajectory_Id == id);
        }
    }
}