using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("FuelConsumption_tbl", Schema = "Service")]
    public class FuelConsumption
    {
        [Key]
        public int Id { get; set; }
        public long BusId { get; set; }
        public int TrajectoryId { get; set; }
        public decimal DistanceKm { get; set; }
        public decimal FuelConsumedL { get; set; }
        public DateTime Timestamp { get; set; }

        [ForeignKey("BusId")]
        public virtual Bus? Bus { get; set; }
    }
}