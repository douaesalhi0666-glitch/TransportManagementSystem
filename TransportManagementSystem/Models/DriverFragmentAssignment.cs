using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("DriverFragmentAssignment_tbl", Schema = "Assignment")]
    public class DriverFragmentAssignment
    {
        [Key]
        public int Assignment_Id { get; set; }
        public long Driver_Id { get; set; }
        public int Fragment_Id { get; set; }
        public DateTime Start_DateTime { get; set; }
        public DateTime? End_DateTime { get; set; }
        public string Status { get; set; } = "Active";

        [ForeignKey("Driver_Id")]
        public virtual Driver? Driver { get; set; }

        [ForeignKey("Fragment_Id")]
        public virtual TrajectoryFragment? Fragment { get; set; }
    }
}