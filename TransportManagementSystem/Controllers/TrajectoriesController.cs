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

            var fragments = await _context.TrajectoryFragments
                .Where(f => f.Trajectory_Id == id && f.Status == "Active")
                .Include(f => f.FragmentStops!)
                    .ThenInclude(fs => fs.TrajectoryStop)
                .ToListAsync();

            var stopsList = new List<object>();

            foreach (var fragment in fragments)
            {
                if (fragment.FragmentStops != null)
                {
                    foreach (var fragmentStop in fragment.FragmentStops.OrderBy(fs => fs.Stop_Order))
                    {
                        stopsList.Add(new
                        {
                            fragment_Id = fragment.Fragment_Id,
                            fragment_Code = fragment.Fragment_Code,
                            fragment_Name = fragment.Fragment_Name,
                            total_Workers = fragment.Total_Workers,
                            ts_Id = fragmentStop.TrajectoryStop?.TS_Id,
                            ts_Name = fragmentStop.TrajectoryStop?.TS_Name ?? "Arrêt inconnu",
                            ts_OrderIndex = fragmentStop.Stop_Order,
                            ts_Latitude = fragmentStop.TrajectoryStop?.TS_Latitude,
                            ts_Longitude = fragmentStop.TrajectoryStop?.TS_Longitude,
                            workers_At_Stop = fragmentStop.Workers_At_Stop
                        });
                    }
                }
            }

            return Ok(new
            {
                trajectoryId = id,
                trajectoryName = trajectory?.Trajectory_Name ?? "Inconnu",
                trajectoryCode = trajectory?.Trajectory_Code ?? "",
                stopsCount = stopsList.Count,
                stops = stopsList
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

                if (trajectory == null)
                {
                    return NotFound();
                }

                string trajectoryName = trajectory.Trajectory_Name;

                var fragments = await _context.TrajectoryFragments
                    .Where(f => f.Trajectory_Id == id)
                    .ToListAsync();

                foreach (var fragment in fragments)
                {
                    var fragmentStops = await _context.FragmentStops
                        .Where(fs => fs.Fragment_Id == fragment.Fragment_Id)
                        .ToListAsync();
                    if (fragmentStops.Any())
                    {
                        _context.FragmentStops.RemoveRange(fragmentStops);
                    }

                    var busAssignments = await _context.BusFragmentAssignments
                        .Where(ba => ba.Fragment_Id == fragment.Fragment_Id && ba.Status == "Active")
                        .ToListAsync();

                    foreach (var assignment in busAssignments)
                    {
                        assignment.Status = "Ended";
                        assignment.End_DateTime = DateTime.Now;

                        var bus = await _context.Buses.FindAsync(assignment.Bus_Id);
                        if (bus != null)
                        {
                            bus.Bus_CurrentFragmentId = null;
                            bus.Bus_Status = "In Service";
                            bus.CurrentOccupancy = 0;
                        }
                    }
                }

                if (fragments.Any())
                {
                    _context.TrajectoryFragments.RemoveRange(fragments);
                }

                var personnelList = await _context.Personnel
                    .Where(p => p.AssignedTrajectoryId == id)
                    .ToListAsync();

                foreach (var person in personnelList)
                {
                    person.AssignedTrajectoryId = null;
                    person.AssignedFragmentId = null;
                    person.AssignedBusId = null;
                    person.AssignedStopId = null;
                    person.IsAssigned = false;
                }

                var personnelAssignments = await _context.PersonnelTrajectoryAssignments
                    .Where(a => a.PTA_TrajectoryId == id)
                    .ToListAsync();
                if (personnelAssignments.Any())
                {
                    _context.PersonnelTrajectoryAssignments.RemoveRange(personnelAssignments);
                }

                var busTrajAssignments = await _context.BusTrajectoryAssignments
                    .Where(a => a.BTA_TrajectoryId == id)
                    .ToListAsync();
                if (busTrajAssignments.Any())
                {
                    _context.BusTrajectoryAssignments.RemoveRange(busTrajAssignments);
                }

                var busFragmentAssignments = await _context.BusFragmentAssignments
                    .Include(bf => bf.Fragment)
                    .Where(bf => bf.Fragment != null && bf.Fragment.Trajectory_Id == id)
                    .ToListAsync();
                if (busFragmentAssignments.Any())
                {
                    _context.BusFragmentAssignments.RemoveRange(busFragmentAssignments);
                }

                var driverFragmentAssignments = await _context.DriverFragmentAssignments
                    .Include(df => df.Fragment)
                    .Where(df => df.Fragment != null && df.Fragment.Trajectory_Id == id)
                    .ToListAsync();
                if (driverFragmentAssignments.Any())
                {
                    _context.DriverFragmentAssignments.RemoveRange(driverFragmentAssignments);
                }

                var alerts = await _context.Alerts.Where(a => a.Alert_TrajectoryId == id).ToListAsync();
                if (alerts.Any()) _context.Alerts.RemoveRange(alerts);

                var schedules = await _context.TrajectorySchedules.Where(s => s.TSched_TrajectoryId == id).ToListAsync();
                if (schedules.Any()) _context.TrajectorySchedules.RemoveRange(schedules);

                var driverPerformances = await _context.DriverPerformance_tbl.Where(p => p.Trajectory_Id == id).ToListAsync();
                if (driverPerformances.Any()) _context.DriverPerformance_tbl.RemoveRange(driverPerformances);

                // RecommendationLogs supprimé - plus utilisé

                var stops = await _context.TrajectoryStops.Where(s => s.TS_TrajectoryId == id).ToListAsync();
                if (stops.Any()) _context.TrajectoryStops.RemoveRange(stops);

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