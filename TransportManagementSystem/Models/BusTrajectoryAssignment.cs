using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("BusTrajectoryAssignment_tbl", Schema = "Assignment")]
    public class BusTrajectoryAssignment
    {
        [Key]
        public int BTA_Id { get; set; }
        public long BTA_BusId { get; set; }
        public int BTA_TrajectoryId { get; set; }
        public DateTime BTA_StartDateTime { get; set; }
        public DateTime? BTA_EndDateTime { get; set; }
        public string BTA_Status { get; set; } = "Active";
    }
}