using System.Collections.Generic;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.ViewModels
{
    public class BusOccupancyViewModel
    {
        public Bus? Bus { get; set; }        // ← nullable
        public int Occupancy { get; set; }
        public int Capacity { get; set; }
        public List<Personnel> Personnel { get; set; } = new();
    }
}