using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("ArrivalHistory_tbl", Schema = "Service")]
    public class ArrivalHistory
    {
        [Key]
        public int Id { get; set; }
        public int StopId { get; set; }
        public long BusId { get; set; }
        public DateTime ScheduledArrivalTime { get; set; }
        public DateTime ActualArrivalTime { get; set; }
        public int DelayMinutes { get; set; }

        [ForeignKey("StopId")]
        public virtual TrajectoryStop? Stop { get; set; }

        [ForeignKey("BusId")]
        public virtual Bus? Bus { get; set; }
    }
}