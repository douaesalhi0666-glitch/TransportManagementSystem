using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TransportManagementSystem.Services
{
    public class OsrmRoutingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public OsrmRoutingService(string baseUrl = "http://router.project-osrm.org")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Calcule la distance routière en mètres entre deux points.
        /// </summary>
        public async Task<double> GetRouteDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var url = $"{_baseUrl}/route/v1/driving/{lon1},{lat1};{lon2},{lat2}?overview=false";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"OSRM error: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var routes = doc.RootElement.GetProperty("routes");
            if (routes.GetArrayLength() == 0)
                throw new Exception("No route found");

            var distance = routes[0].GetProperty("distance").GetDouble();
            return distance; // en mètres
        }

        /// <summary>
        /// Version synchrone pour les contextes où async est gênant.
        /// </summary>
        public double GetRouteDistanceSync(double lat1, double lon1, double lat2, double lon2)
        {
            return GetRouteDistance(lat1, lon1, lat2, lon2).GetAwaiter().GetResult();
        }
    }
}