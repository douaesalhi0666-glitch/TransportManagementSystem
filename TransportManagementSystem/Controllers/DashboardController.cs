using Microsoft.AspNetCore.Mvc;
using TransportManagementSystem.Data;

namespace TransportManagementSystem.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // This handles /Dashboard/Index
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var name = HttpContext.Session.GetString("UserName");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.UserName = name;
            ViewBag.UserRole = role;

            // Redirect to correct dashboard based on role
            if (role == "Admin")
            {
                return RedirectToAction("AdminDashboard");
            }
            else if (role == "Driver")
            {
                return RedirectToAction("DriverDashboard");
            }
            else if (role == "Personnel")
            {
                return RedirectToAction("PersonnelDashboard");
            }

            return RedirectToAction("Login", "Account");
        }

        // This handles /Dashboard/AdminDashboard
        public IActionResult AdminDashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.PersonnelCount = _context.Personnel.Count();
            ViewBag.DriverCount = _context.Drivers.Count();
            ViewBag.BusCount = _context.Buses.Count();
            ViewBag.TrajectoryCount = _context.Trajectories.Count();
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            return View();
        }

        // This handles /Dashboard/DriverDashboard
        public IActionResult DriverDashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Driver")
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }

        // This handles /Dashboard/PersonnelDashboard
        public IActionResult PersonnelDashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Personnel")
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }
    }
}