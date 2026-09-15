using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Drogueria.Data;
using Drogueria.Models;

namespace Drogueria.Controllers
{
    [Authorize(Roles = "Cliente,Administrador,Empleado")]
    public class TiendaController : Controller
    {
        private readonly AppDbContex _context;
        private const string CarritoSessionKey = "Carrito";

        public TiendaController(AppDbContex context)
        {
            _context = context;
        }

        // ── CATÁLOGO ─────────────────────────────────────────

        // GET: /Tienda/Catalogo
        public async Task<IActionResult> Catalogo(string? buscar, Guid? categoriaId)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Estado && p.Stock > 0);

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(p => p.Nombre.Contains(buscar) || (p.Descripcion != null && p.Descripcion.Contains(buscar)));

            if (categoriaId.HasValue && categoriaId.Value != Guid.Empty)
                query = query.Where(p => p.CategoriaId == categoriaId.Value);

            var productos = await query.OrderBy(p => p.Nombre).ToListAsync();

            ViewBag.Categorias = await _context.Categorias
                .Where(c => c.Estado)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            ViewBag.BuscarActual = buscar;
            ViewBag.CategoriaActual = categoriaId;
            ViewData["Title"] = "Catálogo";
            ViewData["ActiveNav"] = "catalogo";

            // Cantidad de items en carrito para el badge
            ViewBag.CarritoCount = ObtenerCarrito().Sum(c => c.Cantidad);

            return View("~/Views/Tienda/_Catalogo.cshtml", productos);
        }

        // ── CARRITO ──────────────────────────────────────────

        // POST: /Tienda/AgregarAlCarrito
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarAlCarrito(Guid productoId, int cantidad = 1)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         Request.Headers["Accept"].ToString().Contains("application/json");

            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null || !producto.Estado)
            {
                if (isAjax)
                    return Json(new { success = false, message = "Producto no disponible." });

                TempData["Error"] = "Producto no disponible.";
                return RedirectToAction(nameof(Catalogo));
            }

            var carrito = ObtenerCarrito();
            var itemExistente = carrito.FirstOrDefault(c => c.ProductoId == productoId);

            if (itemExistente != null)
            {
                itemExistente.Cantidad += cantidad;
            }
            else
            {
                carrito.Add(new CarritoItem
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Imagen = producto.Imagen,
                    Precio = producto.Precio,
                    Cantidad = cantidad
                });
            }

            GuardarCarrito(carrito);
            var totalItems = carrito.Sum(c => c.Cantidad);

            if (isAjax)
            {
                return Json(new { 
                    success = true, 
                    message = $"'{producto.Nombre}' añadido al carrito.", 
                    totalItems,
                    totalCarrito = carrito.Sum(c => c.Subtotal)
                });
            }

            TempData["Exito"] = $"'{producto.Nombre}' agregado al carrito.";
            return RedirectToAction(nameof(Catalogo));
        }

        // GET: /Tienda/Carrito
        public IActionResult Carrito()
        {
            var carrito = ObtenerCarrito();
            ViewData["Title"] = "Mi Carrito";
            ViewData["ActiveNav"] = "carrito";
            return View("~/Views/Tienda/_Carrito.cshtml", carrito);
        }

        // POST: /Tienda/EliminarDelCarrito
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarDelCarrito(Guid productoId)
        {
            var carrito = ObtenerCarrito();
            carrito.RemoveAll(c => c.ProductoId == productoId);
            GuardarCarrito(carrito);
            TempData["Exito"] = "Producto eliminado del carrito.";
            return RedirectToAction(nameof(Carrito));
        }

        // POST: /Tienda/ActualizarCantidad
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarCantidad(Guid productoId, int cantidad)
        {
            if (cantidad < 1) cantidad = 1;
            var carrito = ObtenerCarrito();
            var item = carrito.FirstOrDefault(c => c.ProductoId == productoId);
            if (item != null)
            {
                item.Cantidad = cantidad;
                GuardarCarrito(carrito);
            }
            return RedirectToAction(nameof(Carrito));
        }

        // POST: /Tienda/VaciarCarrito
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VaciarCarrito()
        {
            GuardarCarrito(new List<CarritoItem>());
            TempData["Exito"] = "Carrito vaciado.";
            return RedirectToAction(nameof(Carrito));
        }

        // ── PEDIDOS ──────────────────────────────────────────

        // POST: /Tienda/RealizarPedido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RealizarPedido()
        {
            var carrito = ObtenerCarrito();
            if (!carrito.Any())
            {
                TempData["Error"] = "Tu carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
                return RedirectToAction("Index", "Home");

            var pedido = new Pedido
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Fecha = DateTime.Now,
                Estado = "Pendiente",
                Total = carrito.Sum(c => c.Subtotal)
            };

            foreach (var item in carrito)
            {
                pedido.Detalles.Add(new DetallePedido
                {
                    Id = Guid.NewGuid(),
                    PedidoId = pedido.Id,
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Precio,
                    Subtotal = item.Subtotal
                });
            }

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Limpiar carrito
            GuardarCarrito(new List<CarritoItem>());

            TempData["Exito"] = "¡Pedido realizado con éxito!";
            return RedirectToAction(nameof(MisPedidos));
        }

        // GET: /Tienda/MisPedidos
        public async Task<IActionResult> MisPedidos()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var pedidos = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            ViewData["Title"] = "Mis Pedidos";
            ViewData["ActiveNav"] = "pedidos";
            return View("~/Views/Tienda/_MisPedidos.cshtml", pedidos);
        }

        // ── HELPERS SESIÓN ───────────────────────────────────

        private List<CarritoItem> ObtenerCarrito()
        {
            var json = HttpContext.Session.GetString(CarritoSessionKey);
            if (string.IsNullOrEmpty(json)) return new List<CarritoItem>();
            return JsonSerializer.Deserialize<List<CarritoItem>>(json) ?? new List<CarritoItem>();
        }

        private void GuardarCarrito(List<CarritoItem> carrito)
        {
            var json = JsonSerializer.Serialize(carrito);
            HttpContext.Session.SetString(CarritoSessionKey, json);
        }
    }
}
