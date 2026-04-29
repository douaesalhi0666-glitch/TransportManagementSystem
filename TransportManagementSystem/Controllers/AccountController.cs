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

        // GET: Login Page
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserRole") != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        // POST: Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Check in Admin table
            var admin = await _context.Admin_tbl
                .FirstOrDefaultAsync(a => a.Admin_Email == email);

            if (admin != null && admin.Admin_PasswordHash == password)
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserName", admin.Admin_Name);
                return RedirectToAction("Index", "Dashboard");
            }

            // Check in Drivers table
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Driver_Email == email);

            if (driver != null)
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Driver");
                HttpContext.Session.SetString("UserName", driver.Driver_FirstName + " " + driver.Driver_LastName);
                HttpContext.Session.SetString("DriverId", driver.Driver_id.ToString());
                return RedirectToAction("Index", "Dashboard");
            }

            // Check in Personnel table
            var personnel = await _context.Personnel
                .FirstOrDefaultAsync(p => p.Personnel_Email == email);

            if (personnel != null)
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Personnel");
                HttpContext.Session.SetString("UserName", personnel.Personnel_FirstName + " " + personnel.Personnel_LastName);
                HttpContext.Session.SetString("PersonnelId", personnel.Personnel_Id.ToString());
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Email ou mot de passe incorrect";
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