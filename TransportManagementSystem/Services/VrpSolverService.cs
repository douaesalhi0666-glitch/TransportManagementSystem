using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Services
{
    public class VrpSolverService
    {
        private readonly OsrmRoutingService _routing;

        public VrpSolverService(OsrmRoutingService routing)
        {
            _routing = routing;
        }

        /// <summary>
        /// Résout le VRP avec recuit simulé.
        /// </summary>
        /// <param name="stops">Liste des arrêts (avec nombre de passagers)</param>
        /// <param name="depot">Coordonnées du dépôt (SEWS)</param>
        /// <param name="capacity">Capacité d'un bus</param>
        /// <param name="maxIterations">Nombre d'itérations du recuit</param>
        /// <returns>Liste des tournées (chaque tournée est une liste d'arrêts)</returns>
        public async Task<List<List<TrajectoryStop>>> SolveVrp(
            List<(TrajectoryStop Stop, int PassengerCount)> stops,
            (double lat, double lng) depot,
            int capacity,
            int maxIterations = 10000)
        {
            // 1. Construction des clusters initiaux (glouton)
            var routes = BuildInitialRoutes(stops, depot, capacity);
            var currentDistance = await TotalDistance(routes, depot);
            var bestRoutes = routes.Select(r => r.ToList()).ToList();
            var bestDistance = currentDistance;

            var rand = new Random();
            double temperature = 1000.0;
            double coolingRate = 0.995;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                // Copie des routes pour modification
                var newRoutes = bestRoutes.Select(r => r.ToList()).ToList();

                // Opération de voisinage : déplacer un stop d'une route à une autre
                if (newRoutes.Count > 1)
                {
                    int routeIdx1 = rand.Next(newRoutes.Count);
                    int routeIdx2 = rand.Next(newRoutes.Count);
                    if (routeIdx1 != routeIdx2 && newRoutes[routeIdx1].Count > 0)
                    {
                        int stopIdx = rand.Next(newRoutes[routeIdx1].Count);
                        var stop = newRoutes[routeIdx1][stopIdx];
                        var passengerCount = stops.First(s => s.Stop.TS_Id == stop.TS_Id).PassengerCount;

                        // Vérifier capacité avant de déplacer
                        int newLoad = newRoutes[routeIdx2].Sum(s => stops.First(st => st.Stop.TS_Id == s.TS_Id).PassengerCount) + passengerCount;
                        if (newLoad <= capacity)
                        {
                            newRoutes[routeIdx1].RemoveAt(stopIdx);
                            newRoutes[routeIdx2].Add(stop);
                        }
                    }
                }

                // Supprimer les routes vides
                newRoutes.RemoveAll(r => r.Count == 0);

                var newDist = await TotalDistance(newRoutes, depot);
                double delta = newDist - currentDistance;

                if (delta < 0 || Math.Exp(-delta / temperature) > rand.NextDouble())
                {
                    currentDistance = newDist;
                    bestRoutes = newRoutes;
                    bestDistance = newDist;
                }

                temperature *= coolingRate;
            }

            return bestRoutes;
        }

        private List<List<TrajectoryStop>> BuildInitialRoutes(
            List<(TrajectoryStop Stop, int PassengerCount)> stops,
            (double lat, double lng) depot,
            int capacity)
        {
            var routes = new List<List<TrajectoryStop>>();
            var remaining = stops.ToList();

            while (remaining.Any())
            {
                var currentRoute = new List<TrajectoryStop>();
                int currentLoad = 0;

                for (int i = 0; i < remaining.Count; i++)
                {
                    if (currentLoad + remaining[i].PassengerCount <= capacity)
                    {
                        currentRoute.Add(remaining[i].Stop);
                        currentLoad += remaining[i].PassengerCount;
                        remaining.RemoveAt(i);
                        i--;
                    }
                }
                routes.Add(currentRoute);
            }
            return routes;
        }

        private async Task<double> TotalDistance(List<List<TrajectoryStop>> routes, (double lat, double lng) depot)
        {
            double total = 0;
            foreach (var route in routes)
            {
                double dist = 0;
                var prev = depot;
                foreach (var stop in route)
                {
                    dist += await _routing.GetRouteDistance(prev.lat, prev.lng, (double)stop.TS_Latitude, (double)stop.TS_Longitude);
                    prev = ((double)stop.TS_Latitude, (double)stop.TS_Longitude);
                }
                total += dist / 1000; // convertir en km
            }
            return total;
        }
    }
}