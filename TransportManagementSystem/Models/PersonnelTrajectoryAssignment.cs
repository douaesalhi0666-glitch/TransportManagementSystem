using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("PersonnelTrajectoryAssignments_tbl", Schema = "Assignment")]
    public class PersonnelTrajectoryAssignment
    {
        [Key]
        public int PTA_Id { get; set; }

        public long PTA_PersonnelId { get; set; }        // ← long
        public int PTA_TrajectoryId { get; set; }
        public int? PTA_StopId { get; set; }

        public DateTime PTA_EffectiveFromDate { get; set; }
        public DateTime? PTA_EffectiveToDate { get; set; }

        [MaxLength(20)]
        public string PTA_Status { get; set; } = "Active";

        [ForeignKey("PTA_PersonnelId")]
        public virtual Personnel? Personnel { get; set; }

        [ForeignKey("PTA_TrajectoryId")]
        public virtual Trajectory? Trajectory { get; set; }

        [ForeignKey("PTA_StopId")]
        public virtual TrajectoryStop? Stop { get; set; }
    }
}