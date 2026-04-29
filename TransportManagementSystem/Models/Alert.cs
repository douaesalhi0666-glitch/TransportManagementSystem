using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("Alert_tbl", Schema = "Service")]
    public class Alert
    {
        [Key]
        public int Alert_Id { get; set; }

        public int Alert_PersonnelId { get; set; }
        public int Alert_BusId { get; set; }
        public int Alert_TrajectoryId { get; set; }

        [MaxLength(50)]
        public string Alert_Type { get; set; } = string.Empty; // "500m" ou "200m"

        [MaxLength(500)]
        public string Alert_Message { get; set; } = string.Empty;

        public DateTime Alert_SentAt { get; set; }

        [MaxLength(50)]
        public string Alert_DeliveryChannel { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Alert_Status { get; set; } = string.Empty;

        [ForeignKey("Alert_PersonnelId")]
        public virtual Personnel? Personnel { get; set; }

        [ForeignKey("Alert_BusId")]
        public virtual Bus? Bus { get; set; }

        [ForeignKey("Alert_TrajectoryId")]
        public virtual Trajectory? Trajectory { get; set; }
    }
}