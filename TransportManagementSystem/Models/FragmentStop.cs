using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportManagementSystem.Models
{
    [Table("FragmentStop_tbl", Schema = "Transport")]
    public class FragmentStop
    {
        [Key]
        public int Stop_Id { get; set; }
        public int Fragment_Id { get; set; }
        public int TS_Id { get; set; }
        public int Stop_Order { get; set; }
        public int Workers_At_Stop { get; set; }

        [ForeignKey("Fragment_Id")]
        public virtual TrajectoryFragment? Fragment { get; set; }

        [ForeignKey("TS_Id")]
        public virtual TrajectoryStop? TrajectoryStop { get; set; }
    }
}