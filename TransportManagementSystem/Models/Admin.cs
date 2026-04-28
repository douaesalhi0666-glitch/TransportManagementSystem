using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("Admin_tbl", Schema = "Security")]
    public class Admin
    {
        [Key]
        public int Admin_Id { get; set; }

        [Required]
        [EmailAddress]
        public string Admin_Email { get; set; } = string.Empty;

        [Required]
        public string Admin_PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Admin_Name { get; set; } = string.Empty;
    }
}