using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("TrajectoryStop_tbl", Schema = "Transport")]
    public class TrajectoryStop
    {
        [Key]
        public int TS_Id { get; set; }

        public int TS_TrajectoryId { get; set; }

        [MaxLength(100)]
        public string TS_Name { get; set; } = string.Empty;

        public int TS_OrderIndex { get; set; }

        public decimal TS_Latitude { get; set; }
        public decimal TS_Longitude { get; set; }

        public TimeSpan? TS_PlannedArrivalTime { get; set; }
        public TimeSpan? TS_PlannedDepartureTime { get; set; }

        [ForeignKey("TS_TrajectoryId")]
        public virtual Trajectory? Trajectory { get; set; }
    }
}