using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("MotorizationRequests_tbl", Schema = "Service")]
    public class MotorizationRequest
    {
        [Key]
        public int Id { get; set; }

        public long PersonnelId { get; set; }

        public bool RequestedIsMotorized { get; set; }

        public DateTime RequestDate { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime? ProcessedDate { get; set; }

        [MaxLength(500)]
        public string? AdminComment { get; set; }

        [ForeignKey("PersonnelId")]
        public virtual Personnel? Personnel { get; set; }
    }
}