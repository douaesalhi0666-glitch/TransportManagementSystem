using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserRole") != null)
                return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, int id, string password)
        {
            // ---------- ADMIN (ID = 1) ----------
            // On ne vérifie plus l'email en dur ; on va directement chercher l'admin dans la base
            var admin = await _context.Admin_tbl
                .FirstOrDefaultAsync(a => a.Admin_Id == 1 && a.Admin_Email == email);
            if (admin != null && admin.Admin_PasswordHash == password)
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserName", admin.Admin_Name);
                return RedirectToAction("Index", "Dashboard");
            }

            // ---------- DRIVER (vérification ID + email) ----------
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Driver_Email == email && d.Driver_id == id);
            if (driver != null)
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Driver");
                HttpContext.Session.SetString("UserName", $"{driver.Driver_FirstName} {driver.Driver_LastName}");
                HttpContext.Session.SetString("DriverId", driver.Driver_id.ToString());
                return RedirectToAction("Index", "Dashboard");
            }

            // ---------- PERSONNEL (vérification ID + email) ----------
            var personnel = await _context.Personnel
                .FirstOrDefaultAsync(p => p.Personnel_Email == email && p.Personnel_Id == id);
            if (personnel != null)
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Personnel");
                HttpContext.Session.SetString("UserName", $"{personnel.Personnel_FirstName} {personnel.Personnel_LastName}");
                HttpContext.Session.SetString("PersonnelId", personnel.Personnel_Id.ToString());
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Email ou identifiant incorrect";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}