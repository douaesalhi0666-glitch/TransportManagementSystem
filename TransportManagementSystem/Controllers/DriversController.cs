using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;
using TransportManagementSystem.Services;

namespace TransportManagementSystem.Controllers
{
    public class DriversController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public DriversController(ApplicationDbContext context)
        {
            _context = context;
            _emailService = new EmailService();
        }

        // GET: Drivers
        public async Task<IActionResult> Index()
        {
            var drivers = await _context.Drivers.ToListAsync();
            return View(drivers);
        }

        // GET: Drivers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Drivers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Driver driver)
        {
            if (ModelState.IsValid)
            {
                // Check if ID already exists
                var existingDriver = await _context.Drivers.FindAsync(driver.Driver_id);
                if (existingDriver != null)
                {
                    ModelState.AddModelError("Driver_id", "Cet ID existe déjà. Veuillez entrer un ID unique.");
                    return View(driver);
                }

                // Hash the password if provided
                if (!string.IsNullOrEmpty(driver.Password))
                {
                    driver.Driver_PasswordHash = PasswordService.HashPassword(driver.Password);
                }

                // Generate reset token for email
                driver.Driver_ResetToken = PasswordService.GenerateResetToken();
                driver.Driver_ResetTokenExpiry = DateTime.Now.AddHours(24);
                driver.Driver_EmailConfirmed = false;
                driver.Driver_CreatedAt = DateTime.Now;
                driver.Driver_UpdatedAt = DateTime.Now;

                _context.Add(driver);
                await _context.SaveChangesAsync();

                // Send reset password email
                if (!string.IsNullOrEmpty(driver.Driver_Email))
                {
                    var baseUrl = "https://localhost:7137";
                    var resetLink = $"{baseUrl}/Account/ResetPassword?token={driver.Driver_ResetToken}&email={driver.Driver_Email}&role=driver";
                    await _emailService.SendResetPasswordEmail(driver.Driver_Email, driver.Driver_FirstName + " " + driver.Driver_LastName, resetLink);
                }

                return RedirectToAction(nameof(Index));
            }
            return View(driver);
        }

        // GET: Drivers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null)
            {
                return NotFound();
            }
            return View(driver);
        }

        // POST: Drivers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Driver driver)
        {
            if (id != driver.Driver_id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDriver = await _context.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Driver_id == id);
                    if (existingDriver != null)
                    {
                        driver.Driver_PasswordHash = existingDriver.Driver_PasswordHash;
                        driver.Driver_ResetToken = existingDriver.Driver_ResetToken;
                        driver.Driver_ResetTokenExpiry = existingDriver.Driver_ResetTokenExpiry;
                        driver.Driver_EmailConfirmed = existingDriver.Driver_EmailConfirmed;
                        driver.Driver_CreatedAt = existingDriver.Driver_CreatedAt;
                        driver.Driver_UpdatedAt = DateTime.Now;

                        _context.Update(driver);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DriverExists(driver.Driver_id))
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
            return View(driver);
        }

        // GET: Drivers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(m => m.Driver_id == id);
            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        // POST: Drivers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver != null)
            {
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DriverExists(int id)
        {
            return _context.Drivers.Any(e => e.Driver_id == id);
        }
    }
}