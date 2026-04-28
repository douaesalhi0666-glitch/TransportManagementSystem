using Microsoft.AspNetCore.Mvc;

namespace TransportManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly string AdminEmail = "admin@transport.com";
        private readonly string AdminPassword = "admin123";

        // GET: Login page
        public IActionResult Index()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        public IActionResult Index(string Email, string Password)
        {
            if (Email == AdminEmail && Password == AdminPassword)
            {
                HttpContext.Session.SetString("AdminLoggedIn", "true");
                HttpContext.Session.SetString("AdminEmail", Email);
                // Redirect to your personnel page
                return Redirect("https://localhost:7137/Personnel");
            }
            else
            {
                ViewBag.Error = "Email ou mot de passe incorrect";
                return View();
            }
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}