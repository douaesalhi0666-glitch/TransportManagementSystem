using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("RecommendationLog_tbl", Schema = "Service")]
    public class RecommendationLog
    {
        [Key]
        public int Recommendation_Id { get; set; }

        public DateTime Recommendation_Date { get; set; }

        public long Recommended_DriverId { get; set; }

        public long Recommended_BusId { get; set; }

        public int Recommended_TrajectoryId { get; set; }

        public decimal Score { get; set; }

        public bool Was_Accepted { get; set; }
    }
}