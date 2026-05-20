using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class AnomalyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnomalyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Anomaly/Index
        public async Task<IActionResult> Index()
        {
            var anomalies = await _context.AnomalyLogs
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            return View(anomalies);
        }

        // GET: Anomaly/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var anomaly = await _context.AnomalyLogs.FindAsync(id);
            if (anomaly == null)
                return NotFound();

            return View(anomaly);
        }

        // POST: Anomaly/Resolve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var anomaly = await _context.AnomalyLogs.FindAsync(id);
            if (anomaly == null)
                return NotFound();

            anomaly.IsResolved = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Anomalie marquée comme résolue.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Anomaly/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var anomaly = await _context.AnomalyLogs.FindAsync(id);
            if (anomaly != null)
            {
                _context.AnomalyLogs.Remove(anomaly);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Anomalie supprimée.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}