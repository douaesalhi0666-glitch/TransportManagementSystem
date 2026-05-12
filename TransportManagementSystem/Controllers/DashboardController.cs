using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;
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

        // ================================================
        // GESTION DES NOTIFICATIONS (avec destinataire)
        // ================================================

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
                {
                    notifications = notifications.Skip(notifications.Count - 49).ToList();
                }

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

        // ================================================
        // DEMANDES DE MOTORISATION
        // ================================================

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

            // Vérifier si une demande est déjà en attente
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

                // Désassigner complètement le personnel
                request.Personnel.AssignedTrajectoryId = null;
                request.Personnel.AssignedStopId = null;
                request.Personnel.AssignedFragmentId = null;
                request.Personnel.AssignedBusId = null;
                request.Personnel.IsAssigned = false;

                // Si le personnel devient non motorisé, on l'assigne au point de ramassage le plus proche
                if (!request.RequestedIsMotorized)
                {
                    bool assigned = await AssignToNearestStop(request.Personnel);
                    if (assigned)
                    {
                        AddNotification("Motorization", "Demande acceptée",
                            $"Vous êtes maintenant non motorisé. Vous avez été assigné au point de ramassage le plus proche.",
                            request.PersonnelId);
                    }
                    else
                    {
                        AddNotification("Motorization", "Demande acceptée (sans assignation)",
                            $"Vous êtes non motorisé mais aucun point de ramassage proche n'a été trouvé. Contactez l'administrateur.",
                            request.PersonnelId);
                    }
                }
                else
                {
                    AddNotification("Motorization", "Demande acceptée",
                        $"Votre demande pour devenir motorisé a été acceptée par l'administrateur.",
                        request.PersonnelId);
                }
            }
            else
            {
                request.Status = "Rejected";
                AddNotification("Motorization", "Demande refusée",
                    $"Votre demande pour devenir {(request.RequestedIsMotorized ? "motorisé" : "non motorisé")} a été refusée. Raison : {model.Comment ?? "non précisée"}.",
                    request.PersonnelId);
            }
            request.ProcessedDate = DateTime.Now;
            request.AdminComment = model.Comment;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = model.Approve ? "Demande approuvée." : "Demande refusée." });
        }

        // ================================================
        // MÉTHODES PRIVÉES POUR L'AFFECTATION AU POINT LE PLUS PROCHE
        // ================================================

        private async Task<bool> AssignToNearestStop(Personnel personnel)
        {
            if (personnel == null) return false;
            if (!personnel.Personnel_Latitude.HasValue || !personnel.Personnel_Longitude.HasValue)
                return false;

            // Récupérer tous les arrêts (TrajectoryStop)
            var stops = await _context.TrajectoryStops.ToListAsync();
            if (!stops.Any()) return false;

            double personnelLat = (double)personnel.Personnel_Latitude.Value;
            double personnelLng = (double)personnel.Personnel_Longitude.Value;

            TrajectoryStop? nearestStop = null;
            double minDistance = double.MaxValue;

            foreach (var stop in stops)
            {
                double stopLat = (double)stop.TS_Latitude;
                double stopLng = (double)stop.TS_Longitude;
                double distance = CalculateDistance(personnelLat, personnelLng, stopLat, stopLng);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestStop = stop;
                }
            }

            if (nearestStop == null) return false;

            personnel.AssignedStopId = nearestStop.TS_Id;
            personnel.AssignedTrajectoryId = nearestStop.TS_TrajectoryId;
            personnel.IsAssigned = true;

            await _context.SaveChangesAsync();
            return true;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371e3; // mètres
            var φ1 = lat1 * Math.PI / 180;
            var φ2 = lat2 * Math.PI / 180;
            var Δφ = (lat2 - lat1) * Math.PI / 180;
            var Δλ = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
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