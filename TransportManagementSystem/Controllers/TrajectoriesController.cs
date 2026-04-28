using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class TrajectoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrajectoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Trajectories
        public async Task<IActionResult> Index()
        {
            var trajectories = await _context.Trajectories.ToListAsync();
            return View(trajectories);
        }

        // GET: Trajectories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Trajectories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Trajectory trajectory)
        {
            if (ModelState.IsValid)
            {
                trajectory.Trajectory_CreatedAt = DateTime.Now;
                trajectory.Trajectory_UpdatedAt = DateTime.Now;
                _context.Add(trajectory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(trajectory);
        }

        // GET: Trajectories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trajectory = await _context.Trajectories.FindAsync(id);
            if (trajectory == null)
            {
                return NotFound();
            }
            return View(trajectory);
        }

        // POST: Trajectories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Trajectory trajectory)
        {
            if (id != trajectory.Trajectory_Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    trajectory.Trajectory_UpdatedAt = DateTime.Now;
                    _context.Update(trajectory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrajectoryExists(trajectory.Trajectory_Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(trajectory);
        }

        // GET: Trajectories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trajectory = await _context.Trajectories
                .FirstOrDefaultAsync(m => m.Trajectory_Id == id);
            if (trajectory == null)
            {
                return NotFound();
            }

            return View(trajectory);
        }

        // POST: Trajectories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trajectory = await _context.Trajectories.FindAsync(id);
            if (trajectory != null)
            {
                _context.Trajectories.Remove(trajectory);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TrajectoryExists(int id)
        {
            return _context.Trajectories.Any(e => e.Trajectory_Id == id);
        }
    }
}