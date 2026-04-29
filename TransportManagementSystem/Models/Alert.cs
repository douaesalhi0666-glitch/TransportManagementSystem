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
        public string Alert_Type { get; set; } // "500m" ou "200m"
        public string Alert_Message { get; set; }
        public DateTime Alert_SentAt { get; set; }
        public string Alert_DeliveryChannel { get; set; }
        public string Alert_Status { get; set; }
        // Clés étrangères (optionnelles)
        [ForeignKey("Alert_PersonnelId")]
        public Personnel Personnel { get; set; }
        [ForeignKey("Alert_BusId")]
        public Bus Bus { get; set; }
        [ForeignKey("Alert_TrajectoryId")]
        public Trajectory Trajectory { get; set; }
    }
}