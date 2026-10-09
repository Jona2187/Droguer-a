using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Drogueria.Data;
using Drogueria.Models;

namespace Drogueria.Controllers
{
    [Authorize(Roles = "Administrador,Repartidor")]
    public class RepartidorController : Controller
    {
        private readonly AppDbContex _context;

        public RepartidorController(AppDbContex context)
        {
            _context = context;
        }

        // GET: /Repartidor/MisDomicilios  — Pedidos ACTIVOS (excluye Entregado)
        public async Task<IActionResult> MisDomicilios()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var pedidos = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(p => p.RepartidorId == userId && p.Estado != "Entregado")
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            ViewData["Title"] = "Mis Domicilios";
            ViewData["ActiveNav"] = "misdomicilios";

            return View("~/Views/Repartidor/MisDomicilios.cshtml", pedidos);
        }

        // GET: /Repartidor/Historial — Pedidos ENTREGADOS (historial completo)
        public async Task<IActionResult> Historial()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var pedidos = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(p => p.RepartidorId == userId && p.Estado == "Entregado")
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            ViewData["Title"] = "Historial de Entregas";
            ViewData["ActiveNav"] = "historial";

            return View("~/Views/Repartidor/Historial.cshtml", pedidos);
        }

        // POST: /Repartidor/CambiarEstado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(Guid id, string estado, string? codigoConfirmacion)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id && p.RepartidorId == userId);

            if (pedido == null)
            {
                var errorMsg = "Pedido no encontrado o no asignado a tu cuenta.";
                if (isAjax) return Json(new { success = false, message = errorMsg });
                TempData["Error"] = errorMsg;
                return RedirectToAction(nameof(MisDomicilios));
            }

            var estadosValidos = new[] { "En camino", "Entregado" };
            if (!estadosValidos.Contains(estado))
            {
                var errorMsg = "Estado no permitido para el repartidor.";
                if (isAjax) return Json(new { success = false, message = errorMsg });
                TempData["Error"] = errorMsg;
                return RedirectToAction(nameof(MisDomicilios));
            }

            // Validar código de entrega de 6 dígitos al cambiar a estado Entregado
            if (estado == "Entregado")
            {
                var codigoLimpio = (codigoConfirmacion ?? "").Trim();
                if (string.IsNullOrEmpty(codigoLimpio))
                {
                    var errorMsg = "Debes ingresar el código de confirmación de 6 dígitos entregado por el cliente.";
                    if (isAjax) return Json(new { success = false, message = errorMsg });
                    TempData["Error"] = errorMsg;
                    return RedirectToAction(nameof(MisDomicilios));
                }

                if (!string.IsNullOrEmpty(pedido.CodigoConfirmacion) &&
                    !string.Equals(pedido.CodigoConfirmacion.Trim(), codigoLimpio, StringComparison.OrdinalIgnoreCase))
                {
                    var errorMsg = "El código de confirmación es incorrecto. Pídele al cliente el código de 6 dígitos que figura en su pedido.";
                    if (isAjax) return Json(new { success = false, message = errorMsg });
                    TempData["Error"] = errorMsg;
                    return RedirectToAction(nameof(MisDomicilios));
                }
            }

            pedido.Estado = estado;
            _context.Pedidos.Update(pedido);
            await _context.SaveChangesAsync();

            var successMsg = $"El pedido ha sido marcado como '{estado}' exitosamente.";
            if (isAjax)
            {
                return Json(new { success = true, message = successMsg, id = pedido.Id, estado = pedido.Estado });
            }

            TempData["Success"] = successMsg;
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
