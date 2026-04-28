using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;
using TransportManagementSystem.Services;  // Pour PasswordService

namespace TransportManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Login Page
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserRole") != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        // POST: Login (avec mot de passe pour l'admin uniquement)
        [HttpPost]
        public async Task<IActionResult> Login(string email, int id, string password)
        {
            // ---------- ADMIN (ID = 1) ----------
            if (id == 1 && email == "admin@transport.com")
            {
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Admin_Id == 1);
                if (admin != null && !string.IsNullOrEmpty(password) && PasswordService.VerifyPassword(password, admin.Admin_PasswordHash))
                {
                    HttpContext.Session.SetString("UserEmail", email);
                    HttpContext.Session.SetString("UserRole", "Admin");
                    HttpContext.Session.SetString("UserName", "Administrateur");
                    return RedirectToAction("Index", "Dashboard");
                }
                ViewBag.Error = "Email ou mot de passe administrateur incorrect";
                return View();
            }

            // ---------- DRIVER (sans mot de passe) ----------
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Driver_Email == email);
            if (driver != null && driver.Driver_id == id)
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Driver");
                HttpContext.Session.SetString("UserName", driver.Driver_FirstName + " " + driver.Driver_LastName);
                HttpContext.Session.SetString("DriverId", driver.Driver_id.ToString());
                return RedirectToAction("Index", "Dashboard");
            }

            // ---------- PERSONNEL (sans mot de passe) ----------
            var personnel = await _context.Personnel.FirstOrDefaultAsync(p => p.Personnel_Email == email);
            if (personnel != null && personnel.Personnel_Id == id)
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Personnel");
                HttpContext.Session.SetString("UserName", personnel.Personnel_FirstName + " " + personnel.Personnel_LastName);
                HttpContext.Session.SetString("PersonnelId", personnel.Personnel_Id.ToString());
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Email ou identifiant incorrect";
            return View();
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}