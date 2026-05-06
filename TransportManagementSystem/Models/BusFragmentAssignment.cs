using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("BusFragmentAssignment_tbl", Schema = "Assignment")]
    public class BusFragmentAssignment
    {
        [Key]
        public int Assignment_Id { get; set; }
        public long Bus_Id { get; set; }
        public int Fragment_Id { get; set; }
        public DateTime Start_DateTime { get; set; }
        public DateTime? End_DateTime { get; set; }
        public string Status { get; set; } = "Active";

        [ForeignKey("Bus_Id")]
        public virtual Bus? Bus { get; set; }

        [ForeignKey("Fragment_Id")]
        public virtual TrajectoryFragment? Fragment { get; set; }
    }
}