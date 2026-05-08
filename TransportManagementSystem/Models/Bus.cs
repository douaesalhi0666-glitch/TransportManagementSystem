using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("Bus_tbl", Schema = "Transport")]
    public class Bus
    {
        [Key]
        public long Bus_Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Bus_Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Bus_PlateNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Bus_Model { get; set; }

        [MaxLength(50)]
        public string? Bus_Brand { get; set; }

        public int? Bus_Capacity { get; set; }

        public int? Bus_Year { get; set; }

        [MaxLength(50)]
        public string Bus_Status { get; set; } = "In Service";

        public long? Bus_CurrentDriverId { get; set; }

        public int? Bus_CurrentFragmentId { get; set; }

        public int? CurrentOccupancy { get; set; }

        [Column(TypeName = "decimal(10,8)")]
        public decimal? Bus_CurrentLatitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal? Bus_CurrentLongitude { get; set; }

        public DateTime? Bus_LastLocationUpdateTime { get; set; }

        public DateTime? Bus_CreatedAt { get; set; }

        public DateTime? Bus_UpdatedAt { get; set; }

        [ForeignKey("Bus_CurrentDriverId")]
        public virtual Driver? CurrentDriver { get; set; }

        [ForeignKey("Bus_CurrentFragmentId")]
        public virtual TrajectoryFragment? AssignedFragment { get; set; }

        public virtual ICollection<Personnel>? AssignedPersonnel { get; set; }
    }
}