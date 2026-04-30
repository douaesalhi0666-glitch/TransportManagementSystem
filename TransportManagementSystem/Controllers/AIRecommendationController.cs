using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;
using TransportManagementSystem.Services;

namespace TransportManagementSystem.Controllers
{
    public class AIRecommendationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AssignmentOptimizer _optimizer;

        public AIRecommendationController(ApplicationDbContext context)
        {
            _context = context;
            _optimizer = new AssignmentOptimizer(context);
        }

        // GET: AIRecommendation (this handles both /AIRecommendation and /AIRecommendation/Index)
        public async Task<IActionResult> Index()
        {
            var recommendations = await _optimizer.GetBestAssignments();
            return View(recommendations);
        }

        // POST: Apply recommendation
        [HttpPost]
        public async Task<IActionResult> ApplyRecommendation(long driverId, long busId, int trajectoryId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            var bus = await _context.Buses.FindAsync(busId);
            var trajectory = await _context.Trajectories.FindAsync(trajectoryId);

            if (driver == null || bus == null || trajectory == null)
            {
                TempData["Error"] = "Erreur lors de l'application de la recommandation.";
                return RedirectToAction(nameof(Index));
            }

            driver.Driver_AssignedBusId = busId;
            driver.Driver_Status = "On Route";
            bus.Bus_CurrentDriverId = driverId;
            bus.Bus_CurrentTrajectoryId = trajectoryId;

            var log = new RecommendationLog
            {
                Recommendation_Date = DateTime.Now,
                Recommended_DriverId = driverId,
                Recommended_BusId = busId,
                Recommended_TrajectoryId = trajectoryId,
                Score = 100,
                Was_Accepted = true
            };
            _context.RecommendationLogs.Add(log);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Recommandation appliquée : {driver.Driver_FirstName} {driver.Driver_LastName} → Bus {bus.Bus_Code} → Trajet {trajectory.Trajectory_Name}";
            return RedirectToAction(nameof(Index));
        }

        // POST: Update driver performance after trip
        [HttpPost]
        public async Task<IActionResult> UpdatePerformance(long driverId, int trajectoryId, bool wasOnTime, int delayMinutes)
        {
            var performance = await _context.DriverPerformance_tbl
                .FirstOrDefaultAsync(p => p.Driver_Id == driverId && p.Trajectory_Id == trajectoryId);

            if (performance == null)
            {
                performance = new DriverPerformance
                {
                    Driver_Id = driverId,
                    Trajectory_Id = trajectoryId,
                    TotalTrips = 0,
                    OnTimeTrips = 0,
                    AverageDelayMinutes = 0
                };
                _context.DriverPerformance_tbl.Add(performance);
            }

            performance.TotalTrips++;
            if (wasOnTime)
                performance.OnTimeTrips++;

            double currentAvg = (double)(performance.AverageDelayMinutes ?? 0);
            double newAvg = (currentAvg * (performance.TotalTrips - 1) + delayMinutes) / performance.TotalTrips;
            performance.AverageDelayMinutes = (decimal)newAvg;
            performance.LastTripDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}