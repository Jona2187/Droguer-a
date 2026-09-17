using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Drogueria.Data;
using Drogueria.Models;

namespace Drogueria.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")]
    public class EmpleadoController : Controller
    {
        private readonly AppDbContex _context;

        public EmpleadoController(AppDbContex context)
        {
            _context = context;
        }

        // ── PRODUCTOS (vista de consulta) ─────────────────────
        // GET: /Empleado/Productos
        public async Task<IActionResult> Productos()
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            ViewData["Title"] = "Productos";
            ViewData["ActiveNav"] = "productos";
            ViewBag.Categorias = await _context.Categorias
                .Where(c => c.Estado)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View("~/Views/Empleado/_Productos.cshtml", productos);
        }

        // ── INVENTARIO ────────────────────────────────────────
        // GET: /Empleado/Inventario
        public async Task<IActionResult> Inventario()
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ViewData["Title"] = "Inventario";
            ViewData["ActiveNav"] = "inventario";
            ViewBag.Categorias = await _context.Categorias
                .Where(c => c.Estado)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View("~/Views/Empleado/_Inventario.cshtml", productos);
        }

        // POST: /Empleado/ActualizarStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarStock(Guid id, int stock)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado.";
                return RedirectToAction(nameof(Inventario));
            }

            if (stock < 0) stock = 0;

            producto.Stock = stock;
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Stock de '{producto.Nombre}' actualizado a {stock} unidades.";
            return RedirectToAction(nameof(Inventario));
        }

        // ── PEDIDOS ───────────────────────────────────────────
        // GET: /Empleado/Pedidos
        public async Task<IActionResult> Pedidos()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Repartidor)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            ViewData["Title"] = "Pedidos";
            ViewData["ActiveNav"] = "pedidos";

            ViewBag.Repartidores = await _context.Usuarios
                .Where(u => u.Rol == "Repartidor" && u.Estado)
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            return View("~/Views/Empleado/_Pedidos.cshtml", pedidos);
        }

        // POST: /Empleado/CambiarEstadoPedido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoPedido(Guid id, string estado)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado.";
                return RedirectToAction(nameof(Pedidos));
            }

            var estadosValidos = new[] { "Pendiente", "Confirmado", "En camino", "Entregado", "Cancelado" };
            if (!estadosValidos.Contains(estado))
            {
                TempData["Error"] = "Estado no válido.";
                return RedirectToAction(nameof(Pedidos));
            }

            pedido.Estado = estado;
            _context.Pedidos.Update(pedido);
            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Pedido actualizado a '{estado}'.";
            return RedirectToAction(nameof(Pedidos));
        }

        // POST: /Empleado/AsignarRepartidor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarRepartidor(Guid id, string? repartidorId)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado.";
                return RedirectToAction(nameof(Pedidos));
            }

            if (string.IsNullOrWhiteSpace(repartidorId))
            {
                pedido.RepartidorId = null;
                TempData["Exito"] = "Repartidor desasignado del pedido.";
            }
            else
            {
                var repartidor = await _context.Usuarios.FirstOrDefaultAsync(u => u.Uuid == repartidorId && u.Rol == "Repartidor");
                if (repartidor == null)
                {
                    TempData["Error"] = "El repartidor seleccionado no existe o no es válido.";
                    return RedirectToAction(nameof(Pedidos));
                }

                pedido.RepartidorId = repartidor.Uuid;
                // Si el pedido estaba Pendiente, podemos pasarlo a Confirmado o En camino automáticamente al asignar repartidor
                if (pedido.Estado == "Pendiente")
                {
                    pedido.Estado = "Confirmado";
                }
                TempData["Exito"] = $"Repartidor '{repartidor.Nombre} {repartidor.Apellido}' asignado al pedido.";
            }

            _context.Pedidos.Update(pedido);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Pedidos));
        }
    }
}

