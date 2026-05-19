using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class AnomalyController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AnomalyController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var anomalies = await _context.AnomalyLogs
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
            return View(anomalies);
        }
    }
}