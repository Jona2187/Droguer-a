using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Drogueria.Data;
using Drogueria.Models;

namespace Drogueria.Controllers
{
    [Authorize(Roles = "Repartidor")]
    public class RepartidorController : Controller
    {
        private readonly AppDbContex _context;

        public RepartidorController(AppDbContex context)
        {
            _context = context;
        }

        // GET: /Repartidor/MisDomicilios
        public async Task<IActionResult> MisDomicilios()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var pedidos = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(p => p.RepartidorId == userId)
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            ViewData["Title"] = "Mis Domicilios";
            ViewData["ActiveNav"] = "misdomicilios";

            return View("~/Views/Repartidor/MisDomicilios.cshtml", pedidos);
        }

        // POST: /Repartidor/CambiarEstado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(Guid id, string estado)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id && p.RepartidorId == userId);

            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado o no asignado a tu cuenta.";
                return RedirectToAction(nameof(MisDomicilios));
            }

            var estadosValidos = new[] { "En camino", "Entregado" };
            if (!estadosValidos.Contains(estado))
            {
                TempData["Error"] = "Estado no permitido para el repartidor.";
                return RedirectToAction(nameof(MisDomicilios));
            }

            pedido.Estado = estado;
            _context.Pedidos.Update(pedido);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"El pedido ha pasado a estado '{estado}'.";
            return RedirectToAction(nameof(MisDomicilios));
        }

        // POST: /Repartidor/ActualizarUbicacion
        [HttpPost]
        public async Task<IActionResult> ActualizarUbicacion([FromForm] Guid id, [FromForm] double lat, [FromForm] double lng)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id && p.RepartidorId == userId);

            if (pedido == null)
            {
                return Json(new { success = false, message = "Pedido no encontrado." });
            }

            pedido.LatitudRepartidor = lat;
            pedido.LongitudRepartidor = lng;
            pedido.UltimaUbicacionFecha = DateTime.Now;

            _context.Pedidos.Update(pedido);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
