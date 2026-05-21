using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("Driver_tbl", Schema = "Security")]
    public class Driver
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Display(Name = "ID Chauffeur")]
        [Required(ErrorMessage = "L'ID du chauffeur est requis")]
        public long Driver_id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Prénom")]
        public string Driver_FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Nom")]
        public string Driver_LastName { get; set; } = string.Empty;

        [MaxLength(30)]
        [Display(Name = "Téléphone")]
        public string? Driver_PhoneNumber { get; set; }

        [MaxLength(255)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Driver_Email { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Numéro de permis")]
        public string Driver_LicenseNumber { get; set; } = string.Empty;

        [Display(Name = "Date d'expiration du permis")]
        public DateTime? Driver_LicenseExpiryDate { get; set; }

        // ========== NOUVEAUX CHAMPS ==========

        [Display(Name = "Date d'entrée dans l'entreprise")]
        public DateTime Driver_HireDate { get; set; }

        [MaxLength(20)]
        [Display(Name = "Note / Performance")]
        public string? Driver_Rating { get; set; } = "Bon";

        [Column(TypeName = "decimal(10,8)")]
        [Display(Name = "Latitude domicile")]
        public decimal? Driver_HomeLatitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        [Display(Name = "Longitude domicile")]
        public decimal? Driver_HomeLongitude { get; set; }

        // ========== FIN NOUVEAUX CHAMPS ==========

        [MaxLength(20)]
        [Display(Name = "Statut")]
        public string Driver_Status { get; set; } = "Available";

        [Display(Name = "ID Bus assigné")]
        public long? Driver_AssignedBusId { get; set; }

        [Display(Name = "Date de création")]
        public DateTime? Driver_CreatedAt { get; set; }

        [Display(Name = "Date de modification")]
        public DateTime? Driver_UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey("Driver_AssignedBusId")]
        public virtual Bus? AssignedBus { get; set; }
    }
}