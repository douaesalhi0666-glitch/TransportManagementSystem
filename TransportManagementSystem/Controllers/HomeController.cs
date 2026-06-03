using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult Guide()
        {
            return View();
        }
        public IActionResult DownloadGuide()
        {
            // Chemin vers le fichier PDF que vous aurez placé dans wwwroot/files/
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files/GuideUtilisation_SEWS.pdf");
            if (!System.IO.File.Exists(filePath))
            {
                // Si le fichier n'existe pas, on peut en générer un dynamiquement ou retourner une erreur
                return NotFound("Le guide PDF n'est pas encore disponible. Veuillez contacter l'administrateur.");
            }
            var bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes, "application/pdf", "Guide_Utilisation_SEWS.pdf");
        }
    }
}
