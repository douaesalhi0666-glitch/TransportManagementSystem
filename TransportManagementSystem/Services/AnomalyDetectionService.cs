using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Services
{
    public class AnomalyDetectionService
    {
        private readonly ApplicationDbContext _context;
        private readonly OsrmRoutingService _routing;
        private readonly IsolationForestService _isolationForest;
        private readonly DelayDetectionService _delayService;

        // Constructeur avec toutes les dépendances
        public AnomalyDetectionService(
            ApplicationDbContext context,
            OsrmRoutingService routing,
            IsolationForestService isolationForest,
            DelayDetectionService delayService)
        {
            _context = context;
            _routing = routing;
            _isolationForest = isolationForest;
            _delayService = delayService;
        }

        /// <summary>
        /// Exécute toutes les détections et sauvegarde les anomalies.
        /// </summary>
        public async Task RunFullDetection()
        {
            var anomalies = new List<AnomalyLog>();

            // 1. Détection des déviations de trajectoire (seuil : 0.5 km)
            var deviations = await DetectRouteDeviations(maxDeviationKm: 0.5);
            anomalies.AddRange(deviations);

            // 2. Détection des déviations avancées (Isolation Forest)
            var deviationsAdvanced = await DetectRouteDeviationsAdvanced();
            anomalies.AddRange(deviationsAdvanced);

            // 3. Détection des retards inhabituels
            try
            {
                var delays = await _delayService.DetectUnusualDelays();
                anomalies.AddRange(delays);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur détection retards: {ex.Message}");
            }

            // 4. Anomalie de test (pour vérifier que le système fonctionne)
            anomalies.Add(new AnomalyLog
            {
                Timestamp = DateTime.Now,
                AnomalyType = "Test",
                Description = "Test de détection d'anomalie - Système opérationnel",
                SeverityScore = 0.5
            });

            // Sauvegarde des anomalies
            foreach (var anomaly in anomalies)
                _context.AnomalyLogs.Add(anomaly);

            await _context.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine($"✅ Détection terminée : {anomalies.Count} anomalies trouvées");
        }

        /// <summary>
        /// Détecte les bus qui dévient de leur trajectoire prévue (seuil fixe).
        /// </summary>
        public async Task<List<AnomalyLog>> DetectRouteDeviations(double maxDeviationKm = 1.0)
        {
            var anomalies = new List<AnomalyLog>();
            var buses = await _context.Buses
                .Include(b => b.CurrentTrajectory)
                .Where(b => b.Bus_CurrentLatitude != null && b.Bus_CurrentLongitude != null
                            && b.Bus_CurrentTrajectoryId != null)
                .ToListAsync();

            System.Diagnostics.Debug.WriteLine($"🚌 Buses avec trajectoire: {buses.Count}");

            foreach (var bus in buses)
            {
                System.Diagnostics.Debug.WriteLine($"📍 Bus {bus.Bus_Code} - Lat: {bus.Bus_CurrentLatitude}, Lng: {bus.Bus_CurrentLongitude}");

                var stops = await _context.TrajectoryStops
                    .Where(s => s.TS_TrajectoryId == bus.Bus_CurrentTrajectoryId)
                    .OrderBy(s => s.TS_OrderIndex)
                    .ToListAsync();

                if (!stops.Any()) continue;

                var trajectoryPoints = new List<(double lat, double lng)>();
                trajectoryPoints.Add((34.2900, -6.5700)); // Départ SEWS
                foreach (var s in stops)
                    trajectoryPoints.Add(((double)s.TS_Latitude, (double)s.TS_Longitude));

                double minDistance = double.MaxValue;
                double busLat = (double)bus.Bus_CurrentLatitude!;
                double busLng = (double)bus.Bus_CurrentLongitude!;

                foreach (var pt in trajectoryPoints)
                {
                    double dist = CalculateDistance(busLat, busLng, pt.lat, pt.lng);
                    System.Diagnostics.Debug.WriteLine($"  Distance to ({pt.lat}, {pt.lng}): {dist:F2} km");
                    if (dist < minDistance) minDistance = dist;
                }

                System.Diagnostics.Debug.WriteLine($"🚨 Distance min: {minDistance:F2} km (seuil: {maxDeviationKm} km)");

                if (minDistance > maxDeviationKm)
                {
                    anomalies.Add(new AnomalyLog
                    {
                        Timestamp = DateTime.Now,
                        AnomalyType = "RouteDeviation",
                        Description = $"Bus {bus.Bus_Code} à {minDistance:F2} km de la trajectoire",
                        BusId = bus.Bus_Id,
                        SeverityScore = Math.Min(1.0, minDistance / 5.0)
                    });
                }
            }
            return anomalies;
        }

        /// <summary>
        /// Détecte les bus qui dévient de leur trajectoire prévue (Isolation Forest).
        /// </summary>
        public async Task<List<AnomalyLog>> DetectRouteDeviationsAdvanced()
        {
            var anomalies = new List<AnomalyLog>();
            var buses = await _context.Buses
                .Include(b => b.CurrentTrajectory)
                .Where(b => b.Bus_CurrentLatitude != null && b.Bus_CurrentLongitude != null)
                .ToListAsync();

            foreach (var bus in buses)
            {
                if (bus.Bus_CurrentTrajectoryId == null) continue;

                double lat = (double)bus.Bus_CurrentLatitude!;
                double lng = (double)bus.Bus_CurrentLongitude!;

                bool isAnomaly = await _isolationForest.IsDeviationAnomaly(
                    bus.Bus_Id,
                    bus.Bus_CurrentTrajectoryId.Value,
                    lat, lng);

                if (isAnomaly)
                {
                    anomalies.Add(new AnomalyLog
                    {
                        Timestamp = DateTime.Now,
                        AnomalyType = "RouteDeviation_Advanced",
                        Description = $"Bus {bus.Bus_Code} a une position anormale ({lat:F4}, {lng:F4})",
                        BusId = bus.Bus_Id,
                        SeverityScore = 0.8
                    });
                }
            }
            return anomalies;
        }

        /// <summary>
        /// Calcule la distance orthodromique entre deux points GPS (km).
        /// </summary>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Rayon terrestre en km
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}