using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;
using TransportManagementSystem.Services;

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

        // GET: Reset Password page
        public IActionResult ResetPassword(string token, string email, string role)
        {
            ViewBag.Token = token;
            ViewBag.Email = email;
            ViewBag.Role = role;
            return View();
        }

        // POST: Reset Password
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string email, string role, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Les mots de passe ne correspondent pas.";
                ViewBag.Token = token;
                ViewBag.Email = email;
                ViewBag.Role = role;
                return View();
            }

            if (role == "driver")
            {
                var driver = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.Driver_Email == email && d.Driver_ResetToken == token);

                if (driver == null || driver.Driver_ResetTokenExpiry < DateTime.Now)
                {
                    ViewBag.Error = "Lien invalide ou expiré.";
                    return View();
                }

                driver.Driver_PasswordHash = PasswordService.HashPassword(newPassword);
                driver.Driver_ResetToken = null;
                driver.Driver_ResetTokenExpiry = null;
                driver.Driver_EmailConfirmed = true;
                await _context.SaveChangesAsync();

                ViewBag.Success = "Votre mot de passe a été modifié avec succès.";
            }
            else if (role == "personnel")
            {
                var personnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.Personnel_Email == email && p.Personnel_ResetToken == token);

                if (personnel == null || personnel.Personnel_ResetTokenExpiry < DateTime.Now)
                {
                    ViewBag.Error = "Lien invalide ou expiré.";
                    return View();
                }

                personnel.Personnel_PasswordHash = PasswordService.HashPassword(newPassword);
                personnel.Personnel_ResetToken = null;
                personnel.Personnel_ResetTokenExpiry = null;
                personnel.Personnel_EmailConfirmed = true;
                await _context.SaveChangesAsync();

                ViewBag.Success = "Votre mot de passe a été modifié avec succès.";
            }
            else
            {
                ViewBag.Error = "Rôle invalide.";
            }
            
            return View();
        }
    }
}