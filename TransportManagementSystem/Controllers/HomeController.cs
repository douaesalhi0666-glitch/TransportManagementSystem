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

        // Plus besoin de DownloadGuide car le PDF est généré côté client.
        // Si vous voulez garder une action pour le bouton (optionnel), vous pouvez la laisser commentée.
        // public IActionResult DownloadGuide() { ... }
    }
}