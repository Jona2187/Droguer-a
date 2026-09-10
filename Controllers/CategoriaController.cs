using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Drogueria.Data;
using Drogueria.Models;

namespace Drogueria.Controllers
{
    public class CategoriaController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Home/pages/_Categorias.cshtml");
        }
    }
}