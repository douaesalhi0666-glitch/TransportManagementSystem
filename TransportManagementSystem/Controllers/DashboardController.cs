using Microsoft.AspNetCore.Mvc;
using TransportManagementSystem.Data;
using System.Text.Json;
using System.IO;

namespace TransportManagementSystem.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

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

        [HttpGet]
        public IActionResult GetNotifications()
        {
            var notifications = GetNotificationsFromFile();
            return Ok(notifications);
        }

        [HttpPost]
        public IActionResult ClearNotifications()
        {
            var filePath = GetNotificationFilePath();
            System.IO.File.WriteAllText(filePath, "[]");
            return Ok();
        }

        private List<Notification> GetNotificationsFromFile()
        {
            var filePath = GetNotificationFilePath();

            if (!System.IO.File.Exists(filePath))
            {
                return new List<Notification>();
            }

            var json = System.IO.File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<Notification>>(json) ?? new List<Notification>();
        }

        private string GetNotificationFilePath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "notifications.json");
        }

        public static void AddNotification(string type, string title, string message)
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "notifications.json");
                List<Notification> notifications = new List<Notification>();

                if (System.IO.File.Exists(filePath))
                {
                    var json = System.IO.File.ReadAllText(filePath);
                    notifications = JsonSerializer.Deserialize<List<Notification>>(json) ?? new List<Notification>();
                }

                if (notifications.Count >= 50)
                {
                    notifications = notifications.Skip(notifications.Count - 49).ToList();
                }

                notifications.Add(new Notification
                {
                    Id = notifications.Count + 1,
                    Type = type,
                    Title = title,
                    Message = message,
                    Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                });

                var newJson = JsonSerializer.Serialize(notifications);
                System.IO.File.WriteAllText(filePath, newJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding notification: {ex.Message}");
            }
        }
    }

    public class Notification
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }
}