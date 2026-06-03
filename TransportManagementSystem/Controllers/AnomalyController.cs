using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
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

        // POST: Anomaly/Resolve/5 (supporte AJAX et requête normale)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var anomaly = await _context.AnomalyLogs.FindAsync(id);
            if (anomaly == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Anomalie non trouvée." });
                return NotFound();
            }

            anomaly.IsResolved = true;
            await _context.SaveChangesAsync();

            // Si la requête est AJAX, retourne un JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = "Anomalie résolue." });

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

        // GET: Anomaly/Export
        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var anomalies = await _context.AnomalyLogs
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Timestamp,Type,Description,BusId,PersonnelId,SeverityScore,IsResolved");
            foreach (var a in anomalies)
            {
                csv.AppendLine($"{a.Id},{a.Timestamp:yyyy-MM-dd HH:mm:ss},{a.AnomalyType},{a.Description},{a.BusId},{a.PersonnelId},{a.SeverityScore},{a.IsResolved}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"anomalies_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }
}