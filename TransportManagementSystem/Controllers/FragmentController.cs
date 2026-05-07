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

            var availableBuses = await _context.Buses
                .Where(b => b.Bus_Status == "In Service")
                .ToListAsync();

            ViewBag.Fragment = fragment;
            ViewBag.AvailableBuses = availableBuses;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignBusToFragment(int fragmentId, long busId, DateTime startTime)
        {
            var result = await _fragmentService.AssignBusToFragment(busId, fragmentId, startTime);

            if (result)
                TempData["Success"] = "Bus assigné au fragment avec succès!";
            else
                TempData["Error"] = "Erreur lors de l'assignation du bus.";

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