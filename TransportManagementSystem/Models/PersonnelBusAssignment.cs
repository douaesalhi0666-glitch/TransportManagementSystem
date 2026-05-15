using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("PersonnelBusAssignment_tbl", Schema = "Assignment")]
    public class PersonnelBusAssignment
    {
        [Key]
        public int PBA_Id { get; set; }

        public long PBA_PersonnelId { get; set; }
        public long PBA_BusId { get; set; }
        public DateTime PBA_AssignedAt { get; set; }
        public DateTime? PBA_UnassignedAt { get; set; }
        public string PBA_Status { get; set; } = "Assigned";

        [ForeignKey("PBA_PersonnelId")]
        public virtual Personnel? Personnel { get; set; }

        [ForeignKey("PBA_BusId")]
        public virtual Bus? Bus { get; set; }
    }
}