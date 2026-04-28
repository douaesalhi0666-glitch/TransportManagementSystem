using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("Trajectory_tbl", Schema = "Transport")]
    public class Trajectory
    {
        [Key]
        public int Trajectory_Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Trajectory_Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Trajectory_Code { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Trajectory_Description { get; set; }

        [Column(TypeName = "decimal(10,8)")]
        public decimal? Trajectory_StartLatitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal? Trajectory_StartLongitude { get; set; }

        [Column(TypeName = "decimal(10,8)")]
        public decimal? Trajectory_EndLatitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal? Trajectory_EndLongitude { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Trajectory_DistanceKm { get; set; }
       

        public int? Trajectory_EstimatedDurationMinutes { get; set; }

        [MaxLength(20)]
        public string Trajectory_Status { get; set; } = "Active";

        public DateTime? Trajectory_CreatedAt { get; set; }

        public DateTime? Trajectory_UpdatedAt { get; set; }
    }
}