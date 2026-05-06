using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("TrajectoryFragment_tbl", Schema = "Transport")]
    public class TrajectoryFragment
    {
        [Key]
        public int Fragment_Id { get; set; }
        public int Trajectory_Id { get; set; }
        public string Fragment_Code { get; set; } = string.Empty;
        public string Fragment_Name { get; set; } = string.Empty;
        public int Total_Workers { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime? Created_At { get; set; }

        [ForeignKey("Trajectory_Id")]
        public virtual Trajectory? Trajectory { get; set; }

        public virtual ICollection<FragmentStop>? FragmentStops { get; set; }
    }
}