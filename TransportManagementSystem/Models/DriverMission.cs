using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("DriverMissions_tbl", Schema = "Service")]
    public class DriverMission
    {
        [Key]
        public int Mission_Id { get; set; }
        public long Driver_Id { get; set; }
        public long Bus_Id { get; set; }
        public DateTime Mission_Date { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = "In Progress";
        public int TotalWorkers { get; set; }
        public int WorkersDropped { get; set; }
    }
} 