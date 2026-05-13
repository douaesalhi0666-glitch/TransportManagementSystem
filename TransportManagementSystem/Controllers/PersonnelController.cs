using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Data;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class PersonnelController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PersonnelController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Personnel
        public async Task<IActionResult> Index()
        {
            var personnel = await _context.Personnel
                .Include(p => p.AssignedTrajectory)
                .Include(p => p.AssignedBus)
                .Include(p => p.AssignedStop)
                .ToListAsync();

            // SAFE: Handle null values when getting trajectories
            ViewBag.Trajectories = await _context.Trajectories.ToListAsync() ?? new List<Trajectory>();

            // SAFE: Handle null values when getting buses
            ViewBag.Buses = await _context.Buses.ToListAsync() ?? new List<Bus>();

            // SAFE: Handle null values when getting pickup points
            ViewBag.PickupPoints = await _context.TrajectoryStops
                .OrderBy(s => s.TS_OrderIndex)
                .ToListAsync() ?? new List<TrajectoryStop>();

            return View(personnel ?? new List<Personnel>());
        }

        // GET: Personnel/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var personnel = await _context.Personnel
                .Include(p => p.AssignedTrajectory)
                .Include(p => p.AssignedBus)
                .Include(p => p.AssignedStop)
                .FirstOrDefaultAsync(m => m.Personnel_Id == id);
            if (personnel == null) return NotFound();

            return View(personnel);
        }

        // GET: Personnel/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Trajectories = await _context.Trajectories.ToListAsync();
            ViewBag.Buses = await _context.Buses.ToListAsync();
            ViewBag.PickupPoints = await _context.TrajectoryStops
                .OrderBy(s => s.TS_OrderIndex)
                .ToListAsync();
            return View();
        }

        // POST: Personnel/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Personnel personnel)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.Personnel.FindAsync(personnel.Personnel_Id);
                if (existing != null)
                {
                    ModelState.AddModelError("Personnel_Id", "Cet ID existe déjà.");
                    ViewBag.Trajectories = await _context.Trajectories.ToListAsync();
                    ViewBag.Buses = await _context.Buses.ToListAsync();
                    ViewBag.PickupPoints = await _context.TrajectoryStops.OrderBy(s => s.TS_OrderIndex).ToListAsync();
                    return View(personnel);
                }

                personnel.Personnel_CreatedAt = DateTime.Now;
                personnel.Personnel_UpdatedAt = DateTime.Now;

                _context.Add(personnel);
                await _context.SaveChangesAsync();

                DashboardController.AddNotification("success", "Personnel ajouté", $"Le personnel {personnel.Personnel_FirstName} {personnel.Personnel_LastName} a été ajouté.");

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Trajectories = await _context.Trajectories.ToListAsync();
            ViewBag.Buses = await _context.Buses.ToListAsync();
            ViewBag.PickupPoints = await _context.TrajectoryStops.OrderBy(s => s.TS_OrderIndex).ToListAsync();
            return View(personnel);
        }

        // API: GET /Personnel/GetPersonnelData/{id}
        [HttpGet]
        public async Task<IActionResult> GetPersonnelData(long id)
        {
            var personnel = await _context.Personnel
                .Include(p => p.AssignedTrajectory)
                .Include(p => p.AssignedBus)
                .Include(p => p.AssignedStop)
                .FirstOrDefaultAsync(p => p.Personnel_Id == id);

            if (personnel == null) return NotFound();

            // SAFE: Return null for nullable values instead of throwing error
            return Ok(new
            {
                personnel.Personnel_Id,
                personnel.Personnel_FirstName,
                personnel.Personnel_LastName,
                personnel.Personnel_Gender,
                personnel.Personnel_DateOfBirth,
                personnel.Personnel_PhoneNumber,
                personnel.Personnel_Email,
                personnel.Personnel_EmployeeCode,
                personnel.Personnel_Department,
                personnel.Personnel_Status,
                personnel.Personnel_Address,
                personnel.Personnel_City,
                Personnel_Latitude = personnel.Personnel_Latitude ?? 0,
                Personnel_Longitude = personnel.Personnel_Longitude ?? 0,
                personnel.HomeAddress,
                personnel.IsAssigned,
                AssignedStopId = personnel.AssignedStopId,
                AssignedTrajectoryId = personnel.AssignedTrajectoryId,
                AssignedBusId = personnel.AssignedBusId,
                IsMotorized = personnel.IsMotorized
            });
        }

        // API: POST /Personnel/UpdatePersonnel
        [HttpPost]
        public async Task<IActionResult> UpdatePersonnel([FromBody] PersonnelUpdateModel model)
        {
            if (model == null || model.Personnel_Id == 0)
                return BadRequest(new { success = false, message = "Données invalides" });

            var personnel = await _context.Personnel.FindAsync(model.Personnel_Id);
            if (personnel == null)
                return NotFound(new { success = false, message = "Personnel non trouvé" });

            string oldStopName = null;
            if (personnel.AssignedStopId != model.AssignedStopId)
            {
                var oldStop = await _context.TrajectoryStops.FindAsync(personnel.AssignedStopId);
                oldStopName = oldStop?.TS_Name;
            }

            personnel.Personnel_FirstName = model.Personnel_FirstName;
            personnel.Personnel_LastName = model.Personnel_LastName;
            personnel.Personnel_Gender = model.Personnel_Gender;
            personnel.Personnel_DateOfBirth = model.Personnel_DateOfBirth;
            personnel.Personnel_PhoneNumber = model.Personnel_PhoneNumber;
            personnel.Personnel_Email = model.Personnel_Email;
            personnel.Personnel_EmployeeCode = model.Personnel_EmployeeCode;
            personnel.Personnel_Department = model.Personnel_Department;
            personnel.Personnel_Status = model.Personnel_Status;
            personnel.Personnel_Address = model.Personnel_Address;
            personnel.Personnel_City = model.Personnel_City;
            personnel.Personnel_Latitude = model.Personnel_Latitude;
            personnel.Personnel_Longitude = model.Personnel_Longitude;
            personnel.HomeAddress = model.HomeAddress;
            personnel.AssignedStopId = model.AssignedStopId;
            personnel.AssignedTrajectoryId = model.AssignedTrajectoryId;
            personnel.AssignedBusId = model.AssignedBusId;
            personnel.IsAssigned = model.IsAssigned;
            personnel.IsMotorized = model.IsMotorized;
            personnel.Personnel_UpdatedAt = DateTime.Now;

            // Si assigné à un point de ramassage, marquer comme assigné
            if (model.AssignedStopId.HasValue && !model.IsAssigned)
            {
                personnel.IsAssigned = true;
            }

            _context.Update(personnel);
            await _context.SaveChangesAsync();

            if (oldStopName != null && model.AssignedStopId != null)
            {
                var newStop = await _context.TrajectoryStops.FindAsync(model.AssignedStopId);
                DashboardController.AddNotification("assignment", "Point de ramassage modifié",
                    $"{personnel.Personnel_FirstName} {personnel.Personnel_LastName} a été réassigné de '{oldStopName}' à '{newStop?.TS_Name}'.");
            }
            else if (model.AssignedStopId != null)
            {
                var newStop = await _context.TrajectoryStops.FindAsync(model.AssignedStopId);
                DashboardController.AddNotification("assignment", "Personnel assigné",
                    $"{personnel.Personnel_FirstName} {personnel.Personnel_LastName} a été assigné au point de ramassage '{newStop?.TS_Name}'.");
            }

            return Ok(new { success = true, message = "Personnel mis à jour avec succès" });
        }

        // GET: Personnel/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();
            var personnel = await _context.Personnel
                .Include(p => p.AssignedTrajectory)
                .Include(p => p.AssignedBus)
                .Include(p => p.AssignedStop)
                .FirstOrDefaultAsync(m => m.Personnel_Id == id);
            if (personnel == null) return NotFound();
            return View(personnel);
        }

        // POST: Personnel/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var personnel = await _context.Personnel.FindAsync(id);
            if (personnel != null)
            {
                string personnelName = $"{personnel.Personnel_FirstName} {personnel.Personnel_LastName}";
                _context.Personnel.Remove(personnel);
                await _context.SaveChangesAsync();

                DashboardController.AddNotification("delete", "Personnel supprimé",
                    $"Le personnel {personnelName} a été supprimé.");
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PersonnelExists(long id) => _context.Personnel.Any(e => e.Personnel_Id == id);
    }

    public class PersonnelUpdateModel
    {
        public long Personnel_Id { get; set; }
        public string Personnel_FirstName { get; set; } = string.Empty;
        public string Personnel_LastName { get; set; } = string.Empty;
        public string Personnel_Gender { get; set; } = string.Empty;
        public DateTime? Personnel_DateOfBirth { get; set; }
        public string Personnel_PhoneNumber { get; set; } = string.Empty;
        public string Personnel_Email { get; set; } = string.Empty;
        public string Personnel_EmployeeCode { get; set; } = string.Empty;
        public string Personnel_Department { get; set; } = string.Empty;
        public string Personnel_Status { get; set; } = string.Empty;
        public string Personnel_Address { get; set; } = string.Empty;
        public string Personnel_City { get; set; } = string.Empty;
        public decimal? Personnel_Latitude { get; set; }
        public decimal? Personnel_Longitude { get; set; }
        public string HomeAddress { get; set; } = string.Empty;
        public int? AssignedStopId { get; set; }
        public int? AssignedTrajectoryId { get; set; }
        public long? AssignedBusId { get; set; }
        public bool IsAssigned { get; set; }
        public bool IsMotorized { get; set; } = false;
    }
}