using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using System.IO;
using System.Text.Json;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;
using TransportManagementSystem.Services;

namespace TransportManagementSystem.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAssignmentService _assignmentService;

        public DashboardController(ApplicationDbContext context, IAssignmentService assignmentService)
        {
            _context = context;
            _assignmentService = assignmentService;
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
                return RedirectToAction("AdminDashboard");
            else if (role == "Driver")
                return RedirectToAction("DriverDashboard");
            else if (role == "Personnel")
                return RedirectToAction("PersonnelDashboard");

            return RedirectToAction("Login", "Account");
        }

        public IActionResult AdminDashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin")
                return RedirectToAction("Login", "Account");

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
                return RedirectToAction("Login", "Account");

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }

        // ========== PERSONNEL DASHBOARD ==========
        public async Task<IActionResult> PersonnelDashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Personnel")
                return RedirectToAction("Login", "Account");

            var personnelIdStr = HttpContext.Session.GetString("PersonnelId");
            if (string.IsNullOrEmpty(personnelIdStr))
                return RedirectToAction("Login", "Account");

            var personnelId = long.Parse(personnelIdStr);
            var personnel = await _context.Personnel
                .Include(p => p.AssignedTrajectory)
                .Include(p => p.AssignedStop)
                .Include(p => p.AssignedBus)
                    .ThenInclude(b => b.CurrentDriver)
                .FirstOrDefaultAsync(p => p.Personnel_Id == personnelId);

            if (personnel == null)
                return NotFound();

            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            if (personnel.IsMotorized)
            {
                ViewBag.IsMotorized = true;
                ViewBag.Message = "Vous êtes motorisé, aucun transport ne vous est assigné.";
                return View();
            }

            if (personnel.AssignedTrajectory != null && personnel.AssignedStop != null && personnel.AssignedBus != null)
            {
                ViewBag.AssignedTrajectory = personnel.AssignedTrajectory;
                ViewBag.AssignedStop = personnel.AssignedStop;
                ViewBag.AssignedBus = personnel.AssignedBus;
                ViewBag.DriverName = personnel.AssignedBus.CurrentDriver != null
                    ? $"{personnel.AssignedBus.CurrentDriver.Driver_FirstName} {personnel.AssignedBus.CurrentDriver.Driver_LastName}"
                    : "Non assigné";
            }
            else
            {
                ViewBag.Message = "Aucune assignation de transport trouvée. Contactez l'administrateur.";
            }

            return View();
        }

        // ========== NOTIFICATIONS ==========
        [HttpGet]
        public IActionResult GetNotifications()
        {
            var notifications = GetNotificationsFromFile();
            var personnelIdStr = HttpContext.Session.GetString("PersonnelId");
            if (!string.IsNullOrEmpty(personnelIdStr))
            {
                var personnelId = long.Parse(personnelIdStr);
                notifications = notifications.Where(n => n.PersonnelId == null || n.PersonnelId == personnelId).ToList();
            }
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
                return new List<Notification>();
            var json = System.IO.File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<Notification>>(json) ?? new List<Notification>();
        }

        private string GetNotificationFilePath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "notifications.json");
        }

        public static void AddNotification(string type, string title, string message, long? personnelId = null)
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
                    notifications = notifications.Skip(notifications.Count - 49).ToList();

                notifications.Add(new Notification
                {
                    Id = notifications.Count + 1,
                    Type = type,
                    Title = title,
                    Message = message,
                    Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    PersonnelId = personnelId
                });

                var newJson = JsonSerializer.Serialize(notifications);
                System.IO.File.WriteAllText(filePath, newJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding notification: {ex.Message}");
            }
        }

        // ========== DEMANDES DE MOTORISATION ==========
        [HttpPost]
        public async Task<IActionResult> RequestMotorizationChange(bool isMotorized)
        {
            var personnelIdStr = HttpContext.Session.GetString("PersonnelId");
            if (string.IsNullOrEmpty(personnelIdStr))
                return Unauthorized(new { success = false, message = "Non connecté" });

            var personnelId = long.Parse(personnelIdStr);
            var personnel = await _context.Personnel.FindAsync(personnelId);
            if (personnel == null)
                return NotFound(new { success = false, message = "Personnel non trouvé" });

            var existing = await _context.MotorizationRequests
                .FirstOrDefaultAsync(r => r.PersonnelId == personnelId && r.Status == "Pending");
            if (existing != null)
                return BadRequest(new { success = false, message = "Vous avez déjà une demande en attente." });

            var request = new MotorizationRequest
            {
                PersonnelId = personnelId,
                RequestedIsMotorized = isMotorized,
                RequestDate = DateTime.Now,
                Status = "Pending"
            };
            _context.MotorizationRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Demande envoyée à l'administrateur." });
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingMotorizationRequests()
        {
            var requests = await _context.MotorizationRequests
                .Include(r => r.Personnel)
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.RequestDate)
                .Select(r => new
                {
                    r.Id,
                    PersonnelId = r.PersonnelId,
                    PersonnelName = r.Personnel != null ? r.Personnel.Personnel_FirstName + " " + r.Personnel.Personnel_LastName : "",
                    RequestedIsMotorized = r.RequestedIsMotorized,
                    RequestDate = r.RequestDate
                })
                .ToListAsync();
            return Ok(requests);
        }

        public class ProcessMotorizationRequestModel
        {
            public int RequestId { get; set; }
            public bool Approve { get; set; }
            public string? Comment { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessMotorizationRequest([FromBody] ProcessMotorizationRequestModel model)
        {
            if (model == null || model.RequestId <= 0)
                return BadRequest(new { success = false, message = "Requête invalide." });

            var request = await _context.MotorizationRequests
                .Include(r => r.Personnel)
                .FirstOrDefaultAsync(r => r.Id == model.RequestId);
            if (request == null)
                return NotFound(new { success = false, message = "Demande non trouvée." });

            if (model.Approve)
            {
                request.Personnel.IsMotorized = request.RequestedIsMotorized;
                request.Status = "Approved";

                request.Personnel.AssignedTrajectoryId = null;
                request.Personnel.AssignedStopId = null;
                request.Personnel.AssignedBusId = null;
                request.Personnel.IsAssigned = false;

                if (!request.RequestedIsMotorized)
                {
                    bool success = await _assignmentService.AutoAssignNonMotorizedPersonnel(request.Personnel);
                    if (!success)
                    {
                        AddNotification("Erreur", "Assignation automatique impossible",
                            "Aucun bus ou arrêt disponible. Contactez l'administrateur.",
                            request.PersonnelId);
                    }
                    else
                    {
                        AddNotification("Assignation", "Nouveau transport assigné",
                            $"Vous avez été assigné à un bus sur la trajectoire {request.Personnel.AssignedTrajectoryId}.",
                            request.PersonnelId);
                    }
                }
                else
                {
                    AddNotification("Motorization", "Demande acceptée",
                        $"Vous êtes maintenant motorisé. Aucun transport ne vous sera assigné.",
                        request.PersonnelId);
                }
            }
            else
            {
                request.Status = "Rejected";
                AddNotification("Motorization", "Demande refusée",
                    $"Votre demande a été refusée. Raison : {model.Comment ?? "non précisée"}.",
                    request.PersonnelId);
            }

            request.ProcessedDate = DateTime.Now;
            request.AdminComment = model.Comment;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = model.Approve ? "Demande approuvée." : "Demande refusée." });
        }

        // ========== NOUVELLE MÉTHODE POUR CHANGER LE STATUT SANS DEMANDE (direct) ==========
        [HttpPost]
        public async Task<IActionResult> ToggleMotorizedStatus()
        {
            var personnelIdStr = HttpContext.Session.GetString("PersonnelId");
            if (string.IsNullOrEmpty(personnelIdStr))
                return Unauthorized();

            var personnelId = long.Parse(personnelIdStr);
            var personnel = await _context.Personnel.FindAsync(personnelId);
            if (personnel == null)
                return NotFound();

            // Inverser le statut sans toucher aux assignations
            personnel.IsMotorized = !personnel.IsMotorized;
            personnel.Personnel_UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, isMotorized = personnel.IsMotorized, message = $"Vous êtes maintenant {(personnel.IsMotorized ? "motorisé" : "non motorisé")}." });
        }
    }

    public class Notification
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public long? PersonnelId { get; set; }
    }
}