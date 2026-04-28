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

        // POST: Login with Email + ID (no password)
        [HttpPost]
        public async Task<IActionResult> Login(string email, int id)
        {
            // Hardcoded Admin (email + ID=1)
            if (email == "admin@transport.com" && id == 1)
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserName", "Administrateur");
                return RedirectToAction("Index", "Dashboard");
            }

            // Check in Drivers table
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Driver_Email == email);
            if (driver != null && driver.Driver_id == id)
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Driver");
                HttpContext.Session.SetString("UserName", driver.Driver_FirstName + " " + driver.Driver_LastName);
                HttpContext.Session.SetString("DriverId", driver.Driver_id.ToString());
                return RedirectToAction("Index", "Dashboard");
            }

            // Check in Personnel table
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