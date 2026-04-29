using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class BusesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BusesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Buses
        public async Task<IActionResult> Index()
        {
            var buses = await _context.Buses.ToListAsync();
            return View(buses);
        }

        // GET: Buses/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Buses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bus bus)
        {
            if (ModelState.IsValid)
            {
                bus.Bus_CreatedAt = DateTime.Now;
                bus.Bus_UpdatedAt = DateTime.Now;
                _context.Add(bus);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(bus);
        }

        // GET: Buses/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bus = await _context.Buses.FindAsync(id);
            if (bus == null)
            {
                return NotFound();
            }
            return View(bus);
        }

        // POST: Buses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Bus bus)
        {
            if (id != bus.Bus_Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    bus.Bus_UpdatedAt = DateTime.Now;
                    _context.Update(bus);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BusExists(bus.Bus_Id))
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
            return View(bus);
        }

        // GET: Buses/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bus = await _context.Buses
                .FirstOrDefaultAsync(m => m.Bus_Id == id);
            if (bus == null)
            {
                return NotFound();
            }

            return View(bus);
        }

        // POST: Buses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var bus = await _context.Buses.FindAsync(id);
            if (bus != null)
            {
                _context.Buses.Remove(bus);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BusExists(long id)
        {
            return _context.Buses.Any(e => e.Bus_Id == id);
        }
    }
}