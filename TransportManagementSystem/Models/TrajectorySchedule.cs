using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("TrajectoryShedule_tbl", Schema = "Transport")]
    public class TrajectorySchedule
    {
        [Key]
        public int TSched_Id { get; set; }
        public int TSched_TrajectoryId { get; set; }
        public string TSched_DayOfWeek { get; set; } = string.Empty;
        public TimeSpan TSched_DepartureTime { get; set; }
        public TimeSpan? TSched_ReturnTime { get; set; }
    }
}