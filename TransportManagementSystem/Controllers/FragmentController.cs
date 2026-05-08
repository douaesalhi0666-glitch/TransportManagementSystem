using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;
using TransportManagementSystem.Services;

namespace TransportManagementSystem.Controllers
{
    public class FragmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FragmentService _fragmentService;

        public FragmentController(ApplicationDbContext context)
        {
            _context = context;
            _fragmentService = new FragmentService(context);
        }

        public async Task<IActionResult> Index()
        {
            var trajectories = await _context.Trajectories
                .Where(t => t.Trajectory_Status == "Active")
                .ToListAsync();
            ViewBag.Trajectories = trajectories;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateFragments(int trajectoryId, int busCapacity = 20)
        {
            var fragments = await _fragmentService.GenerateFragments(trajectoryId, busCapacity);

            if (!fragments.Any())
            {
                TempData["Error"] = "Aucun fragment généré. Vérifiez que des personnels sont assignés aux arrêts.";
                return RedirectToAction("Index");
            }

            TempData["Fragments"] = System.Text.Json.JsonSerializer.Serialize(fragments);
            TempData["TrajectoryId"] = trajectoryId;

            return RedirectToAction("FragmentsResult");
        }

        public IActionResult FragmentsResult()
        {
            if (TempData["Fragments"] == null)
                return RedirectToAction("Index");

            var fragmentsJson = TempData["Fragments"] as string;
            var fragments = System.Text.Json.JsonSerializer.Deserialize<List<FragmentResult>>(fragmentsJson ?? "[]");
            ViewBag.TrajectoryId = TempData["TrajectoryId"];

            return View(fragments);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFragments(int trajectoryId, string fragmentsJson)
        {
            var fragments = System.Text.Json.JsonSerializer.Deserialize<List<FragmentResult>>(fragmentsJson);

            if (fragments == null || !fragments.Any())
            {
                TempData["Error"] = "Aucun fragment à sauvegarder.";
                return RedirectToAction("Index");
            }

            var savedFragments = await _fragmentService.SaveFragments(trajectoryId, fragments);

            TempData["Success"] = $"{savedFragments.Count} fragments créés avec succès!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ViewFragments()
        {
            var fragments = await _context.TrajectoryFragments
                .Include(f => f.Trajectory)
                .Include(f => f.FragmentStops)
                    .ThenInclude(fs => fs.TrajectoryStop)
                .ToListAsync();

            var fragmentIds = fragments.Select(f => f.Fragment_Id).ToList();
            var busAssignments = await _context.BusFragmentAssignments
                .Include(bf => bf.Bus)
                .Where(bf => fragmentIds.Contains(bf.Fragment_Id) && bf.Status == "Active")
                .ToDictionaryAsync(bf => bf.Fragment_Id, bf => bf);

            ViewBag.BusAssignments = busAssignments;
            return View(fragments);
        }

        public async Task<IActionResult> AssignBusToFragment(int fragmentId)
        {
            var fragment = await _context.TrajectoryFragments
                .Include(f => f.Trajectory)
                .FirstOrDefaultAsync(f => f.Fragment_Id == fragmentId);

            var currentAssignment = await _context.BusFragmentAssignments
                .Include(a => a.Bus)
                .FirstOrDefaultAsync(a => a.Fragment_Id == fragmentId && a.Status == "Active");

            var assignedBusIds = await _context.BusFragmentAssignments
                .Where(a => a.Status == "Active" && a.Fragment_Id != fragmentId)
                .Select(a => a.Bus_Id)
                .ToListAsync();

            var availableBuses = await _context.Buses
                .Where(b => b.Bus_Status == "In Service" &&
                    (!assignedBusIds.Contains(b.Bus_Id) || (currentAssignment != null && b.Bus_Id == currentAssignment.Bus_Id)))
                .ToListAsync();

            ViewBag.Fragment = fragment;
            ViewBag.AvailableBuses = availableBuses;
            ViewBag.CurrentAssignment = currentAssignment;
            ViewBag.HasExistingAssignment = currentAssignment != null;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignBusToFragment(int fragmentId, long busId, DateTime startTime)
        {
            try
            {
                var currentAssignment = await _context.BusFragmentAssignments
                    .Include(a => a.Bus)
                    .FirstOrDefaultAsync(a => a.Fragment_Id == fragmentId && a.Status == "Active");

                if (currentAssignment != null && currentAssignment.Bus_Id != busId)
                {
                    currentAssignment.Status = "Ended";
                    currentAssignment.End_DateTime = DateTime.Now;

                    var oldBus = await _context.Buses.FindAsync(currentAssignment.Bus_Id);
                    if (oldBus != null)
                    {
                        oldBus.Bus_CurrentFragmentId = null;
                        oldBus.Bus_Status = "In Service";
                        oldBus.CurrentOccupancy = 0;
                        oldBus.Bus_UpdatedAt = DateTime.Now;
                        _context.Buses.Update(oldBus);
                    }

                    _context.BusFragmentAssignments.Update(currentAssignment);
                    await _context.SaveChangesAsync();

                    TempData["Info"] = $"🔄 Bus '{currentAssignment.Bus?.Bus_Code}' libéré avec succès.";
                }

                var existingBusAssignment = await _context.BusFragmentAssignments
                    .FirstOrDefaultAsync(a => a.Bus_Id == busId && a.Status == "Active" && a.Fragment_Id != fragmentId);

                if (existingBusAssignment != null)
                {
                    var existingFragment = await _context.TrajectoryFragments
                        .Where(f => f.Fragment_Id == existingBusAssignment.Fragment_Id)
                        .Select(f => f.Fragment_Name)
                        .FirstOrDefaultAsync();

                    TempData["Error"] = $"❌ Ce bus est déjà assigné au fragment '{existingFragment}'!";
                    return RedirectToAction("ViewFragments");
                }

                var result = await _fragmentService.AssignBusToFragment(busId, fragmentId, startTime);

                if (result)
                {
                    int personnelAssigned = await _fragmentService.AssignPersonnelToBusForFragment(fragmentId, busId);

                    if (personnelAssigned > 0)
                        TempData["Success"] = $"✅ Bus assigné et {personnelAssigned} personnels affectés avec succès!";
                    else
                        TempData["Success"] = "✅ Bus assigné, mais aucun personnel trouvé pour ce fragment.";
                }
                else
                {
                    TempData["Error"] = "❌ Erreur lors de l'assignation du bus.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Erreur: {ex.Message}";
            }

            return RedirectToAction("ViewFragments");
        }

        [HttpPost]
        public async Task<IActionResult> UnassignBusFromFragment(int fragmentId)
        {
            try
            {
                var assignment = await _context.BusFragmentAssignments
                    .Include(a => a.Bus)
                    .FirstOrDefaultAsync(a => a.Fragment_Id == fragmentId && a.Status == "Active");

                if (assignment != null)
                {
                    var personnelOnBus = await _context.Personnel
                        .Where(p => p.AssignedBusId == assignment.Bus_Id)
                        .ToListAsync();

                    foreach (var person in personnelOnBus)
                    {
                        person.AssignedBusId = null;
                        person.AssignedFragmentId = null;
                    }

                    assignment.Status = "Ended";
                    assignment.End_DateTime = DateTime.Now;

                    var bus = await _context.Buses.FindAsync(assignment.Bus_Id);
                    if (bus != null)
                    {
                        bus.Bus_CurrentFragmentId = null;
                        bus.Bus_Status = "In Service";
                        bus.CurrentOccupancy = 0;
                        bus.Bus_UpdatedAt = DateTime.Now;
                        _context.Buses.Update(bus);
                    }

                    _context.BusFragmentAssignments.Update(assignment);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"✅ Bus '{assignment.Bus?.Bus_Code}' désassigné et personnels retirés avec succès!";
                }
                else
                {
                    TempData["Error"] = "❌ Aucun bus assigné trouvé pour ce fragment.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Erreur: {ex.Message}";
            }

            return RedirectToAction("ViewFragments");
        }

        public async Task<IActionResult> FragmentMap(int fragmentId)
        {
            var fragmentData = await _fragmentService.GetFragmentMapData(fragmentId);

            if (fragmentData == null)
            {
                TempData["Error"] = "Fragment non trouvé.";
                return RedirectToAction("ViewFragments");
            }

            ViewBag.FragmentData = fragmentData;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetFragmentsForTrajectory(int trajectoryId)
        {
            var fragments = await _context.TrajectoryFragments
                .Where(f => f.Trajectory_Id == trajectoryId)
                .Include(f => f.FragmentStops)
                    .ThenInclude(fs => fs.TrajectoryStop)
                .ToListAsync();

            var result = fragments.Select(f => new
            {
                f.Fragment_Id,
                f.Fragment_Name,
                f.Fragment_Code,
                f.Total_Workers,
                Stops = f.FragmentStops
                    .OrderBy(fs => fs.Stop_Order)
                    .Select(fs => new
                    {
                        fs.TrajectoryStop.TS_Id,
                        fs.TrajectoryStop.TS_Name,
                        fs.TrajectoryStop.TS_Latitude,
                        fs.TrajectoryStop.TS_Longitude,
                        fs.TrajectoryStop.TS_OrderIndex,
                        fs.Workers_At_Stop
                    })
            });
            return Ok(result);
        }
    }
}