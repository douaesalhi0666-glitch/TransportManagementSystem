using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("BusLocationHistory_tbl", Schema = "Service")]
    public class BusLocationHistory
    {
        [Key]
        public long Id { get; set; }
        public long BusId { get; set; }
        public int TrajectoryId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime Timestamp { get; set; }

        [ForeignKey("BusId")]
        public virtual Bus? Bus { get; set; }
    }
}