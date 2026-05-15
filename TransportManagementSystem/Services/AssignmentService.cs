using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly ApplicationDbContext _context;

        public AssignmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AutoAssignNonMotorizedPersonnel(Personnel personnel)
        {
            if (personnel.Personnel_Latitude == null || personnel.Personnel_Longitude == null)
                return false;

            // Récupérer tous les arrêts
            var allStops = await _context.TrajectoryStops
                .Include(s => s.Trajectory)
                .ToListAsync();

            if (!allStops.Any()) return false;

            // Trouver l'arrêt le plus proche
            TrajectoryStop? nearestStop = null;
            double minDistance = double.MaxValue;

            foreach (var stop in allStops)
            {
                double distance = CalculateDistance(
                    (double)personnel.Personnel_Latitude,
                    (double)personnel.Personnel_Longitude,
                    (double)stop.TS_Latitude,
                    (double)stop.TS_Longitude
                );

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestStop = stop;
                }
            }

            if (nearestStop == null) return false;

            // Assigner l'arrêt et la trajectoire
            personnel.AssignedStopId = nearestStop.TS_Id;
            personnel.AssignedTrajectoryId = nearestStop.TS_TrajectoryId;

            // Trouver un bus sur cette trajectoire avec de la place
            var busesOnTrajectory = await _context.Buses
                .Where(b => b.Bus_CurrentTrajectoryId == nearestStop.TS_TrajectoryId)
                .ToListAsync();

            if (!busesOnTrajectory.Any()) return false;

            // Compter l'occupation actuelle de chaque bus
            var busOccupancy = new Dictionary<long, int>();
            foreach (var bus in busesOnTrajectory)
            {
                var count = await _context.Personnel
                    .CountAsync(p => p.AssignedBusId == bus.Bus_Id && p.IsAssigned == true);
                busOccupancy[bus.Bus_Id] = count;
            }

            // Choisir le bus avec le moins de personnels (et non plein)
            var bestBus = busesOnTrajectory
                .OrderBy(b => busOccupancy.GetValueOrDefault(b.Bus_Id, 0))
                .FirstOrDefault(b => busOccupancy.GetValueOrDefault(b.Bus_Id, 0) < (b.Bus_Capacity ?? 50));

            if (bestBus == null) return false;

            personnel.AssignedBusId = bestBus.Bus_Id;
            personnel.IsAssigned = true;

            await _context.SaveChangesAsync();
            return true;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // km
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
}