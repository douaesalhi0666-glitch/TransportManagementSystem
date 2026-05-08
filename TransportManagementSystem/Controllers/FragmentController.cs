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
                    .ThenInclude(fs => fs.TrajectoryStop!)
                .ToListAsync();

            return View(fragments);
        }

        public async Task<IActionResult> AssignBusToFragment(int fragmentId)
        {
            var fragment = await _context.TrajectoryFragments
                .Include(f => f.Trajectory)
                .FirstOrDefaultAsync(f => f.Fragment_Id == fragmentId);

            // Find current assignment for this fragment
            var currentAssignment = await _context.BusFragmentAssignments
                .Include(a => a.Bus)
                .FirstOrDefaultAsync(a => a.Fragment_Id == fragmentId && a.Status == "Active");

            // Get IDs of buses already assigned to other active fragments
            var assignedBusIds = await _context.BusFragmentAssignments
                .Where(a => a.Status == "Active" && a.Fragment_Id != fragmentId)
                .Select(a => a.Bus_Id)
                .ToListAsync();

            // Get available buses (including the one currently assigned to this fragment if any)
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
                // Find the current active assignment for this fragment
                var currentAssignment = await _context.BusFragmentAssignments
                    .Include(a => a.Bus)
                    .FirstOrDefaultAsync(a => a.Fragment_Id == fragmentId && a.Status == "Active");

                // If there is a current assignment and it's a different bus, free the old bus
                if (currentAssignment != null && currentAssignment.Bus_Id != busId)
                {
                    // End the current assignment
                    currentAssignment.Status = "Ended";
                    currentAssignment.End_DateTime = DateTime.Now;

                    // Free the old bus
                    var oldBus = await _context.Buses.FindAsync(currentAssignment.Bus_Id);
                    if (oldBus != null)
                    {
                        oldBus.Bus_CurrentFragmentId = null;
                        oldBus.Bus_Status = "In Service";
                        oldBus.Bus_UpdatedAt = DateTime.Now;
                        _context.Buses.Update(oldBus);
                    }

                    _context.BusFragmentAssignments.Update(currentAssignment);
                    await _context.SaveChangesAsync();

                    TempData["Info"] = $"🔄 Bus '{currentAssignment.Bus?.Bus_Code}' libéré avec succès.";
                }

                // Check if the new bus is already assigned to another active fragment (different from current)
                var existingBusAssignment = await _context.BusFragmentAssignments
                    .FirstOrDefaultAsync(a => a.Bus_Id == busId && a.Status == "Active" && a.Fragment_Id != fragmentId);

                if (existingBusAssignment != null)
                {
                    var existingFragment = await _context.TrajectoryFragments
                        .Where(f => f.Fragment_Id == existingBusAssignment.Fragment_Id)
                        .Select(f => f.Fragment_Name)
                        .FirstOrDefaultAsync();

                    TempData["Error"] = $"❌ Ce bus est déjà assigné au fragment '{existingFragment}'! Veuillez choisir un autre bus.";
                    return RedirectToAction("ViewFragments");
                }

                // Check if assignment already exists for this exact pair
                var existingAssignment = await _context.BusFragmentAssignments
                    .FirstOrDefaultAsync(a => a.Bus_Id == busId && a.Fragment_Id == fragmentId && a.Status == "Active");

                if (existingAssignment == null)
                {
                    // Create the new assignment
                    var newAssignment = new BusFragmentAssignment
                    {
                        Bus_Id = busId,
                        Fragment_Id = fragmentId,
                        Start_DateTime = startTime,
                        Status = "Active"
                    };
                    _context.BusFragmentAssignments.Add(newAssignment);
                }

                // Assign the new bus to the fragment
                var newBus = await _context.Buses.FindAsync(busId);
                if (newBus != null)
                {
                    newBus.Bus_CurrentFragmentId = fragmentId;
                    newBus.Bus_Status = "On Route";
                    newBus.Bus_UpdatedAt = DateTime.Now;
                    _context.Buses.Update(newBus);
                }

                await _context.SaveChangesAsync();

                if (currentAssignment != null && currentAssignment.Bus_Id != busId)
                    TempData["Success"] = "✅ Bus réassigné avec succès! L'ancien bus a été libéré.";
                else if (currentAssignment == null)
                    TempData["Success"] = "✅ Bus assigné au fragment avec succès!";
                else
                    TempData["Success"] = "✅ Le même bus reste assigné à ce fragment.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Erreur lors de l'assignation: {ex.Message}";
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