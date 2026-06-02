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

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserRole") != null)
                return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, int id, string password)
        {
            // Admin
            var admin = await _context.Admin_tbl
                .FirstOrDefaultAsync(a => a.Admin_Id == 1 && a.Admin_Email == email);
            if (admin != null && admin.Admin_PasswordHash == password)
            {
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserName", admin.Admin_Name);
                return RedirectToAction("AdminDashboard", "Dashboard");
            }

            // Driver
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Driver_Email == email && d.Driver_id == id);
            if (driver != null)
            {
                HttpContext.Session.SetString("UserRole", "Driver");
                HttpContext.Session.SetString("UserName", $"{driver.Driver_FirstName} {driver.Driver_LastName}");
                HttpContext.Session.SetString("DriverId", driver.Driver_id.ToString());
                return RedirectToAction("DriverDashboard", "Dashboard");
            }

            // Personnel
            var personnel = await _context.Personnel
                .FirstOrDefaultAsync(p => p.Personnel_Email == email && p.Personnel_Id == id);
            if (personnel != null)
            {
                HttpContext.Session.SetString("UserRole", "Personnel");
                HttpContext.Session.SetString("UserName", $"{personnel.Personnel_FirstName} {personnel.Personnel_LastName}");
                HttpContext.Session.SetString("PersonnelId", personnel.Personnel_Id.ToString());
                return RedirectToAction("PersonnelDashboard", "Dashboard");
            }

            ViewBag.Error = "Email ou identifiant incorrect";
            return View();
        }

        [HttpGet]
        public IActionResult Logout()
        {
            // Supprimer la session
            HttpContext.Session.Clear();
            // Supprimer le cookie de session
            HttpContext.Response.Cookies.Delete(".AspNetCore.Session");
            // Forcer les en-têtes anti-cache
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult CheckSession()
        {
            bool isAuthenticated = !string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole"));
            return Json(new { isAuthenticated });
        }
    }
}