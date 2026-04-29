using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class PersonnelController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PersonnelController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Personnel
        public async Task<IActionResult> Index()
        {
            var personnel = await _context.Personnel.ToListAsync();
            return View(personnel);
        }

        // GET: Personnel/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var personnel = await _context.Personnel
                .FirstOrDefaultAsync(m => m.Personnel_Id == id);
            if (personnel == null)
            {
                return NotFound();
            }

            return View(personnel);
        }

        // GET: Personnel/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Personnel/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Personnel personnel)
        {
            if (ModelState.IsValid)
            {
                // Check if ID already exists
                var existing = await _context.Personnel.FindAsync(personnel.Personnel_Id);
                if (existing != null)
                {
                    ModelState.AddModelError("Personnel_Id", "Cet ID existe déjà. Veuillez entrer un ID unique.");
                    return View(personnel);
                }

                personnel.Personnel_CreatedAt = DateTime.Now;
                personnel.Personnel_UpdatedAt = DateTime.Now;

                _context.Add(personnel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(personnel);
        }

        // GET: Personnel/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var personnel = await _context.Personnel.FindAsync(id);
            if (personnel == null)
            {
                return NotFound();
            }
            return View(personnel);
        }

        // POST: Personnel/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Personnel personnel)
        {
            if (id != personnel.Personnel_Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Personnel.AsNoTracking().FirstOrDefaultAsync(p => p.Personnel_Id == id);
                    if (existing != null)
                    {
                        personnel.Personnel_CreatedAt = existing.Personnel_CreatedAt;
                        personnel.Personnel_UpdatedAt = DateTime.Now;
                        _context.Update(personnel);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersonnelExists(personnel.Personnel_Id))
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
            return View(personnel);
        }

        // GET: Personnel/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var personnel = await _context.Personnel
                .FirstOrDefaultAsync(m => m.Personnel_Id == id);
            if (personnel == null)
            {
                return NotFound();
            }

            return View(personnel);
        }

        // POST: Personnel/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var personnel = await _context.Personnel.FindAsync(id);
            if (personnel != null)
            {
                _context.Personnel.Remove(personnel);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PersonnelExists(long id)
        {
            return _context.Personnel.Any(e => e.Personnel_Id == id);
        }
    }
}