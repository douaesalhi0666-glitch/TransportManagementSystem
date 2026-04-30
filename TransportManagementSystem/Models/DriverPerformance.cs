using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("DriverPerformance_tbl", Schema = "Service")]
    public class DriverPerformance
    {
        [Key]
        public int Performance_Id { get; set; }

        public long Driver_Id { get; set; }

        public int Trajectory_Id { get; set; }

        public int TotalTrips { get; set; }

        public int OnTimeTrips { get; set; }

        public decimal? AverageDelayMinutes { get; set; }

        public DateTime? LastTripDate { get; set; }

        [ForeignKey("Driver_Id")]
        public virtual Driver? Driver { get; set; }

        [ForeignKey("Trajectory_Id")]
        public virtual Trajectory? Trajectory { get; set; }
    }
}