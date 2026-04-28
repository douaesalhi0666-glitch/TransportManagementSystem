using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;
using TransportManagementSystem.Services;

namespace TransportManagementSystem.Controllers
{
    public class PersonnelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public PersonnelController(ApplicationDbContext context)
        {
            _context = context;
            _emailService = new EmailService();
        }

        // GET: Personnel
        public async Task<IActionResult> Index()
        {
            var personnel = await _context.Personnel.ToListAsync();
            return View(personnel);
        }

        // GET: Personnel/Details/5
        public async Task<IActionResult> Details(int? id)
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
                var existingPersonnel = await _context.Personnel.FindAsync(personnel.Personnel_Id);
                if (existingPersonnel != null)
                {
                    ModelState.AddModelError("Personnel_Id", "Cet ID existe déjà. Veuillez entrer un ID unique.");
                    return View(personnel);
                }

                // Hash the password if provided
                if (!string.IsNullOrEmpty(personnel.Password))
                {
                    personnel.Personnel_PasswordHash = PasswordService.HashPassword(personnel.Password);
                }

                // Generate reset token for email
                personnel.Personnel_ResetToken = PasswordService.GenerateResetToken();
                personnel.Personnel_ResetTokenExpiry = DateTime.Now.AddHours(24);
                personnel.Personnel_EmailConfirmed = false;
                personnel.Personnel_CreatedAt = DateTime.Now;
                personnel.Personnel_UpdatedAt = DateTime.Now;

                _context.Add(personnel);
                await _context.SaveChangesAsync();

                // Send reset password email
                if (!string.IsNullOrEmpty(personnel.Personnel_Email))
                {
                    var baseUrl = "https://localhost:7137";
                    var resetLink = $"{baseUrl}/Account/ResetPassword?token={personnel.Personnel_ResetToken}&email={personnel.Personnel_Email}&role=personnel";
                    await _emailService.SendResetPasswordEmail(personnel.Personnel_Email, personnel.Personnel_FirstName + " " + personnel.Personnel_LastName, resetLink);
                }

                return RedirectToAction(nameof(Index));
            }
            return View(personnel);
        }

        // GET: Personnel/Edit/5
        public async Task<IActionResult> Edit(int? id)
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
        public async Task<IActionResult> Edit(int id, Personnel personnel)
        {
            if (id != personnel.Personnel_Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPersonnel = await _context.Personnel.AsNoTracking().FirstOrDefaultAsync(p => p.Personnel_Id == id);
                    if (existingPersonnel != null)
                    {
                        personnel.Personnel_PasswordHash = existingPersonnel.Personnel_PasswordHash;
                        personnel.Personnel_ResetToken = existingPersonnel.Personnel_ResetToken;
                        personnel.Personnel_ResetTokenExpiry = existingPersonnel.Personnel_ResetTokenExpiry;
                        personnel.Personnel_EmailConfirmed = existingPersonnel.Personnel_EmailConfirmed;
                        personnel.Personnel_CreatedAt = existingPersonnel.Personnel_CreatedAt;
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
        public async Task<IActionResult> Delete(int? id)
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var personnel = await _context.Personnel.FindAsync(id);
            if (personnel != null)
            {
                _context.Personnel.Remove(personnel);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PersonnelExists(int id)
        {
            return _context.Personnel.Any(e => e.Personnel_Id == id);
        }
    }
}