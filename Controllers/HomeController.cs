using System.Diagnostics;
using Drogueria.Models;
using Microsoft.AspNetCore.Mvc;

namespace Drogueria.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Home/Pagina_Inicio/Index.cshtml");
        }

        public IActionResult Categorias()
        {
            return View("~/Views/Home/Pages/Categorias.cshtml");
        }

        public IActionResult Nosotros()
        {
            return View("~/Views/Home/pages/_Nosotros.cshtml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
