using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("Personnel_tbl", Schema = "Security")]
    public class Personnel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Display(Name = "ID Personnel")]
        [Required(ErrorMessage = "L'ID du personnel est requis")]
        public int Personnel_Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Prénom")]
        public string Personnel_FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Nom")]
        public string Personnel_LastName { get; set; } = string.Empty;

        [MaxLength(20)]
        [Display(Name = "Genre")]
        public string? Personnel_Gender { get; set; }

        [Display(Name = "Date de naissance")]
        public DateTime? Personnel_DateOfBirth { get; set; }

        [MaxLength(30)]
        [Display(Name = "Téléphone")]
        public string? Personnel_PhoneNumber { get; set; }

        [MaxLength(255)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Personnel_Email { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Code employé")]
        public string Personnel_EmployeeCode { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Département")]
        public string? Personnel_Department { get; set; }

        [MaxLength(20)]
        [Display(Name = "Statut")]
        public string Personnel_Status { get; set; } = "Active";

        [MaxLength(500)]
        [Display(Name = "Adresse")]
        public string? Personnel_Address { get; set; }

        [MaxLength(100)]
        [Display(Name = "Ville")]
        public string? Personnel_City { get; set; }

        [Display(Name = "Latitude")]
        public decimal? Personnel_Latitude { get; set; }

        [Display(Name = "Longitude")]
        public decimal? Personnel_Longitude { get; set; }

        // This property is not used anymore - password is stored in Personnel_PasswordHash
        // [Display(Name = "Mot de passe temporaire")]
        // [DataType(DataType.Password)]
        // public string? Password { get; set; }

        // Password management fields
        public string? Personnel_PasswordHash { get; set; }
        public string? Personnel_ResetToken { get; set; }
        public DateTime? Personnel_ResetTokenExpiry { get; set; }
        public bool Personnel_EmailConfirmed { get; set; }

        [Display(Name = "Date de création")]
        public DateTime? Personnel_CreatedAt { get; set; }

        [Display(Name = "Date de modification")]
        public DateTime? Personnel_UpdatedAt { get; set; }
    }
}