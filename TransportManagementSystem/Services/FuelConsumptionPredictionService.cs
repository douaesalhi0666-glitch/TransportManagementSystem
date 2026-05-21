using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Services
{
    public class FuelConsumptionPredictionService
    {
        private readonly ApplicationDbContext _context;
        private double _slope;
        private double _intercept;

        public FuelConsumptionPredictionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Train()
        {
            var data = await _context.FuelConsumptions
                .Where(f => f.DistanceKm > 0 && f.FuelConsumedL > 0)
                .ToListAsync();

            if (data.Count < 10) return;

            // Convertir decimal en double
            double[] distances = data.Select(d => (double)d.DistanceKm).ToArray();
            double[] consumptions = data.Select(d => (double)d.FuelConsumedL).ToArray();

            double avgX = distances.Average();
            double avgY = consumptions.Average();

            double numerator = 0;
            double denominator = 0;
            for (int i = 0; i < distances.Length; i++)
            {
                numerator += (distances[i] - avgX) * (consumptions[i] - avgY);
                denominator += Math.Pow(distances[i] - avgX, 2);
            }

            if (denominator != 0)
            {
                _slope = numerator / denominator;
                _intercept = avgY - _slope * avgX;
            }
        }

        public double PredictFuelConsumption(double distanceKm)
        {
            return _slope * distanceKm + _intercept;
        }

        public async Task<List<AnomalyLog>> DetectAnomalies(double thresholdPercent = 0.30)
        {
            var anomalies = new List<AnomalyLog>();
            var consumptions = await _context.FuelConsumptions
                .Include(f => f.Bus)
                .ToListAsync();

            foreach (var consumption in consumptions)
            {
                double expected = PredictFuelConsumption((double)consumption.DistanceKm);
                double actual = (double)consumption.FuelConsumedL;
                double deviation = Math.Abs(actual - expected) / expected;

                if (deviation > thresholdPercent && consumption.DistanceKm > 5)
                {
                    anomalies.Add(new AnomalyLog
                    {
                        Timestamp = DateTime.Now,
                        AnomalyType = "AbnormalFuelConsumption",
                        Description = $"Bus {consumption.Bus?.Bus_Code ?? consumption.BusId.ToString()} a une consommation anormale : " +
                                      $"{actual:F1}L consommés vs {expected:F1}L attendus (écart {deviation:P0})",
                        BusId = consumption.BusId,
                        SeverityScore = Math.Min(0.9, deviation)
                    });
                }
            }
            return anomalies;
        }
    }
}