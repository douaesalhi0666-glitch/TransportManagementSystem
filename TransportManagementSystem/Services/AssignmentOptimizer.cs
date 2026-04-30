using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Services
{
    public class AssignmentOptimizer
    {
        private readonly ApplicationDbContext _context;

        public AssignmentOptimizer(ApplicationDbContext context)
        {
            _context = context;
        }

        // Main recommendation method
        public async Task<List<AssignmentRecommendation>> GetBestAssignments()
        {
            var recommendations = new List<AssignmentRecommendation>();

            // Get all active trajectories
            var trajectories = await _context.Trajectories
                .Where(t => t.Trajectory_Status == "Active")
                .ToListAsync();

            // Get available drivers
            var availableDrivers = await _context.Drivers
                .Where(d => d.Driver_Status == "Available")
                .Include(d => d.AssignedBus)
                .ToListAsync();

            // Get available buses
            var availableBuses = await _context.Buses
                .Where(b => b.Bus_Status == "In Service" && b.Bus_CurrentDriverId == null)
                .ToListAsync();

            foreach (var trajectory in trajectories)
            {
                // Calculate required bus capacity based on personnel assigned
                var personnelCount = await _context.Personnel
                    .CountAsync(p => p.AssignedTrajectoryId == trajectory.Trajectory_Id && p.IsAssigned == true);

                // Find best bus for this trajectory
                var bestBus = FindBestBus(availableBuses, personnelCount, trajectory);

                if (bestBus != null)
                {
                    // Find best driver for this bus
                    var bestDriver = FindBestDriver(availableDrivers, bestBus, trajectory);

                    if (bestDriver != null)
                    {
                        var score = CalculateScore(bestDriver, bestBus, trajectory);
                        recommendations.Add(new AssignmentRecommendation
                        {
                            Driver = bestDriver,
                            Bus = bestBus,
                            Trajectory = trajectory,
                            Score = score,
                            RequiredCapacity = personnelCount,
                            Reason = GetRecommendationReason(bestDriver, bestBus, trajectory, score)
                        });
                    }
                }
            }

            return recommendations.OrderByDescending(r => r.Score).ToList();
        }

        private Bus? FindBestBus(List<Bus> availableBuses, int personnelCount, Trajectory trajectory)
        {
            if (!availableBuses.Any())
                return null;

            return availableBuses
                .Where(b => (b.Bus_Capacity ?? 50) >= personnelCount)
                .OrderBy(b => Math.Abs((b.Bus_Capacity ?? 50) - personnelCount))
                .FirstOrDefault();
        }

        private Driver? FindBestDriver(List<Driver> availableDrivers, Bus bus, Trajectory trajectory)
        {
            if (!availableDrivers.Any())
                return null;

            return availableDrivers
                .OrderByDescending(d => GetDriverScoreForTrajectory(d, trajectory))
                .ThenBy(d => d.Driver_ExperienceYears ?? 0)
                .FirstOrDefault();
        }

        private double GetDriverScoreForTrajectory(Driver driver, Trajectory trajectory)
        {
            // Calculate driver's historical performance on this trajectory
            var performance = _context.DriverPerformance_tbl
                .FirstOrDefault(p => p.Driver_Id == driver.Driver_id && p.Trajectory_Id == trajectory.Trajectory_Id);

            if (performance == null)
                return 50; // Neutral score

            var onTimeRate = performance.TotalTrips > 0
                ? (double)performance.OnTimeTrips / performance.TotalTrips * 100
                : 50;

            var delayPenalty = Math.Max(0, 100 - ((double)(performance.AverageDelayMinutes ?? 0) * 2));

            return (onTimeRate + delayPenalty) / 2;
        }

        private double CalculateScore(Driver driver, Bus bus, Trajectory trajectory)
        {
            double score = 0;

            // Driver availability and experience (30%)
            var experienceScore = Math.Min(100, (driver.Driver_ExperienceYears ?? 0) * 10);
            score += experienceScore * 0.3;

            // Bus capacity match (20%)
            var personnelCount = _context.Personnel
                .Count(p => p.AssignedTrajectoryId == trajectory.Trajectory_Id && p.IsAssigned == true);

            double capacityMatch;
            if (personnelCount == 0)
                capacityMatch = 100;
            else
                capacityMatch = (bus.Bus_Capacity ?? 50) >= personnelCount ? 100 :
                    (double)(bus.Bus_Capacity ?? 50) / personnelCount * 100;

            score += capacityMatch * 0.2;

            // Historical performance (50%)
            var performanceScore = GetDriverScoreForTrajectory(driver, trajectory);
            score += performanceScore * 0.5;

            return Math.Round(score, 2);
        }

        private string GetRecommendationReason(Driver driver, Bus bus, Trajectory trajectory, double score)
        {
            var reasons = new List<string>();

            if ((driver.Driver_ExperienceYears ?? 0) >= 5)
                reasons.Add($"Expérimenté ({driver.Driver_ExperienceYears} ans)");

            var performance = _context.DriverPerformance_tbl
                .FirstOrDefault(p => p.Driver_Id == driver.Driver_id && p.Trajectory_Id == trajectory.Trajectory_Id);

            if (performance != null && performance.TotalTrips > 0)
            {
                var onTimeRate = (double)performance.OnTimeTrips / performance.TotalTrips * 100;
                if (onTimeRate >= 80)
                    reasons.Add($"Excellent sur ce trajet ({onTimeRate:F0}% ponctuel)");
            }

            var personnelCount = _context.Personnel
                .Count(p => p.AssignedTrajectoryId == trajectory.Trajectory_Id && p.IsAssigned == true);

            if ((bus.Bus_Capacity ?? 50) >= personnelCount)
                reasons.Add($"Capacité adaptée ({bus.Bus_Capacity} places pour {personnelCount} personnes)");
            else if (personnelCount > 0)
                reasons.Add($"Capacité limite ({bus.Bus_Capacity} places pour {personnelCount} personnes)");

            return reasons.Count > 0 ? string.Join(", ", reasons) : "Bonne correspondance générale";
        }
    }

    public class AssignmentRecommendation
    {
        public Driver? Driver { get; set; }
        public Bus? Bus { get; set; }
        public Trajectory? Trajectory { get; set; }
        public double Score { get; set; }
        public int RequiredCapacity { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}