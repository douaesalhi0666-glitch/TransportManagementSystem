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

        public AnomalyDetectionService(ApplicationDbContext context, OsrmRoutingService routing)
        {
            _context = context;
            _routing = routing;
        }

        /// <summary>
        /// Détecte les bus qui dévient de leur trajectoire prévue.
        /// </summary>
        public async Task<List<AnomalyLog>> DetectRouteDeviations(double maxDeviationKm = 1.0)
        {
            var anomalies = new List<AnomalyLog>();
            var buses = await _context.Buses
                .Include(b => b.CurrentTrajectory)
                .Where(b => b.Bus_CurrentLatitude != null && b.Bus_CurrentLongitude != null
                            && b.Bus_CurrentTrajectoryId != null)
                .ToListAsync();

            foreach (var bus in buses)
            {
                // Récupérer la liste des points de la trajectoire (début + arrêts)
                var stops = await _context.TrajectoryStops
                    .Where(s => s.TS_TrajectoryId == bus.Bus_CurrentTrajectoryId)
                    .OrderBy(s => s.TS_OrderIndex)
                    .ToListAsync();

                if (!stops.Any()) continue;

                var trajectoryPoints = new List<(double lat, double lng)>();
                // Départ SEWS
                trajectoryPoints.Add((34.2900, -6.5700));
                foreach (var s in stops)
                    trajectoryPoints.Add(((double)s.TS_Latitude, (double)s.TS_Longitude));

                // Distance minimale entre la position du bus et n'importe quel point de la trajectoire
                double minDistance = double.MaxValue;
                foreach (var pt in trajectoryPoints)
                {
                    var dist = await _routing.GetRouteDistance(
                        (double)bus.Bus_CurrentLatitude.Value,
                        (double)bus.Bus_CurrentLongitude.Value,
                        pt.lat, pt.lng);
                    if (dist < minDistance) minDistance = dist;
                }

                if (minDistance / 1000 > maxDeviationKm)
                {
                    anomalies.Add(new AnomalyLog
                    {
                        Timestamp = DateTime.Now,
                        AnomalyType = "RouteDeviation",
                        Description = $"Bus {bus.Bus_Code} à {minDistance / 1000:F2} km de la trajectoire",
                        BusId = bus.Bus_Id,
                        SeverityScore = Math.Min(1.0, minDistance / 1000 / 5.0) // score entre 0 et 1
                    });
                }
            }
            return anomalies;
        }

        /// <summary>
        /// Détecte les personnels motorisés qui utilisent le bus (log d'assignation + présence).
        /// Ici, on suppose qu'un personnel motorisé ne doit jamais avoir AssignedBusId != null.
        /// </summary>
        public async Task<List<AnomalyLog>> DetectMotorizedUsingBus()
        {
            var anomalies = new List<AnomalyLog>();
            var motorizedWithBus = await _context.Personnel
                .Where(p => p.IsMotorized == true && p.AssignedBusId != null)
                .ToListAsync();

            foreach (var p in motorizedWithBus)
            {
                anomalies.Add(new AnomalyLog
                {
                    Timestamp = DateTime.Now,
                    AnomalyType = "MotorizedUsingBus",
                    Description = $"Personnel motorisé {p.Personnel_FirstName} {p.Personnel_LastName} a un bus assigné (ID {p.AssignedBusId})",
                    PersonnelId = p.Personnel_Id,
                    SeverityScore = 0.9
                });
            }
            return anomalies;
        }

        /// <summary>
        /// Exécute toutes les détections et sauvegarde les anomalies.
        /// </summary>
        public async Task RunFullDetection()
        {
            var deviations = await DetectRouteDeviations();
            var motorized = await DetectMotorizedUsingBus();
            var all = deviations.Concat(motorized).ToList();

            foreach (var anomaly in all)
                _context.AnomalyLogs.Add(anomaly);

            await _context.SaveChangesAsync();
        }
    }
}