using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("AnomalyLog_tbl", Schema = "Service")]
    public class AnomalyLog
    {
        [Key]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string AnomalyType { get; set; } = string.Empty; // "Deviation", "MotorizedUsingBus", ...
        public string Description { get; set; } = string.Empty;
        public long? BusId { get; set; }
        public long? PersonnelId { get; set; }
        public double SeverityScore { get; set; }
        public bool IsResolved { get; set; } = false;
    }
}