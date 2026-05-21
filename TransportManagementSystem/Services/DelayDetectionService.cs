using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Services
{
    public class DelayDetectionService
    {
        private readonly ApplicationDbContext _context;

        public DelayDetectionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AnomalyLog>> DetectUnusualDelays(double stdDevMultiplier = 2.0)
        {
            var anomalies = new List<AnomalyLog>();

            var arrivalHistory = await _context.ArrivalHistories
                .Include(a => a.Bus)
                .Include(a => a.Stop)
                .ToListAsync();

            var grouped = arrivalHistory.GroupBy(a => a.StopId);

            foreach (var group in grouped)
            {
                var delays = group.Select(a => (double)a.DelayMinutes).ToList();
                if (delays.Count < 5) continue;

                double mean = delays.Average();
                double stdDev = Math.Sqrt(delays.Average(v => Math.Pow(v - mean, 2)));

                var recentArrivals = group.OrderByDescending(a => a.ScheduledArrivalTime).Take(5);
                foreach (var arrival in recentArrivals)
                {
                    if (Math.Abs(arrival.DelayMinutes - mean) > stdDevMultiplier * stdDev)
                    {
                        anomalies.Add(new AnomalyLog
                        {
                            Timestamp = DateTime.Now,
                            AnomalyType = "UnusualDelay",
                            Description = $"Retard inhabituel à l'arrêt '{arrival.Stop?.TS_Name ?? arrival.StopId.ToString()}': " +
                                          $"{arrival.DelayMinutes} min (moyenne {mean:F1} min)",
                            BusId = arrival.BusId,
                            SeverityScore = Math.Min(0.9, Math.Abs(arrival.DelayMinutes - mean) / (mean + 1))
                        });
                    }
                }
            }
            return anomalies;
        }
    }
}