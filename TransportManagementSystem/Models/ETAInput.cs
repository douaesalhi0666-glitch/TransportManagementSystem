using Microsoft.ML.Data;

namespace TransportManagementSystem.Models
{
    public class ETAInput
    {
        public float DistanceKm { get; set; }
        public float HourOfDay { get; set; }
        public float DayOfWeek { get; set; }
        public float IsPeakHour { get; set; }
        public float TrafficLevel { get; set; }
    }

    public class ETAPrediction
    {
        [ColumnName("Score")]
        public float EstimatedMinutes { get; set; }
    }
}