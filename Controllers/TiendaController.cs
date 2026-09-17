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

        // â”€â”€ CATÃLOGO â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

        // â”€â”€ CARRITO â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        // POST: /Tienda/AgregarAlCarrito
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarAlCarrito(Guid productoId, int cantidad = 1)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         Request.Headers["Accept"].ToString().Contains("application/json");

            if (cantidad < 1) cantidad = 1;

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

            int cantidadEnCarrito = itemExistente?.Cantidad ?? 0;
            int nuevaCantidadTotal = cantidadEnCarrito + cantidad;

            // Validar que no exceda el stock disponible
            if (nuevaCantidadTotal > producto.Stock)
            {
                var mensaje = producto.Stock == 0
                    ? $"'{producto.Nombre}' está agotado."
                    : $"Solo hay {producto.Stock} unidades disponibles de '{producto.Nombre}'. Ya tienes {cantidadEnCarrito} en el carrito.";

                if (isAjax)
                    return Json(new { success = false, message = mensaje });

                TempData["Error"] = mensaje;
                return RedirectToAction(nameof(Catalogo));
            }

            if (itemExistente != null)
            {
                itemExistente.Cantidad = nuevaCantidadTotal;
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
                    message = $"'{producto.Nombre}' añadido al carrito ({nuevaCantidadTotal} ud.).", 
                    totalItems,
                    totalCarrito = carrito.Sum(c => c.Subtotal)
                });
            }

            TempData["Exito"] = $"'{producto.Nombre}' agregado al carrito.";
            return RedirectToAction(nameof(Catalogo));
        }

        // GET: /Tienda/Carrito
        public async Task<IActionResult> Carrito()
        {
            var carrito = ObtenerCarrito();

            // Sincronizar stock actual de cada producto
            if (carrito.Any())
            {
                var productosIds = carrito.Select(c => c.ProductoId).ToList();
                var productos = await _context.Productos
                    .Where(p => productosIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p);

                foreach (var item in carrito)
                {
                    if (productos.TryGetValue(item.ProductoId, out var producto))
                    {
                        item.StockDisponible = producto.Stock;
                    }
                }
            }

            // Cargar direcciones del usuario para el selector de entrega
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.Direcciones = await _context.DireccionesUsuario
                .Where(d => d.UsuarioId == usuarioId)
                .OrderByDescending(d => d.EsPredeterminada)
                .ThenByDescending(d => d.FechaCreacion)
                .ToListAsync();

            ViewData["Title"] = "Mi Carrito";
            ViewData["ActiveNav"] = "carrito";
            return View("~/Views/Tienda/_Carrito.cshtml", carrito);
        }

        // POST: /Tienda/EliminarDelCarrito
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarDelCarrito(Guid productoId)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         Request.Headers["Accept"].ToString().Contains("application/json");

            var carrito = ObtenerCarrito();
            carrito.RemoveAll(c => c.ProductoId == productoId);
            GuardarCarrito(carrito);

            if (isAjax)
            {
                return Json(new
                {
                    success = true,
                    message = "Producto eliminado del carrito.",
                    totalItems = carrito.Sum(c => c.Cantidad),
                    totalCarrito = carrito.Sum(c => c.Subtotal)
                });
            }

            TempData["Exito"] = "Producto eliminado del carrito.";
            return RedirectToAction(nameof(Carrito));
        }

        // POST: /Tienda/ActualizarCantidad
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarCantidad(Guid productoId, int cantidad)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         Request.Headers["Accept"].ToString().Contains("application/json");

            if (cantidad < 1) cantidad = 1;

            // Validar stock disponible
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null)
            {
                if (isAjax)
                    return Json(new { success = false, message = "Producto no encontrado." });

                TempData["Error"] = "Producto no encontrado.";
                return RedirectToAction(nameof(Carrito));
            }

            if (cantidad > producto.Stock)
            {
                var mensaje = $"Solo hay {producto.Stock} unidades disponibles de '{producto.Nombre}'.";
                if (isAjax)
                    return Json(new { success = false, message = mensaje, stockDisponible = producto.Stock });

                TempData["Error"] = mensaje;
                return RedirectToAction(nameof(Carrito));
            }

            var carrito = ObtenerCarrito();
            var item = carrito.FirstOrDefault(c => c.ProductoId == productoId);
            if (item != null)
            {
                item.Cantidad = cantidad;
                GuardarCarrito(carrito);
            }

            if (isAjax)
            {
                return Json(new
                {
                    success = true,
                    totalItems = carrito.Sum(c => c.Cantidad),
                    subtotalItem = item?.Subtotal ?? 0,
                    totalCarrito = carrito.Sum(c => c.Subtotal)
                });
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

        // â”€â”€ PEDIDOS â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        // POST: /Tienda/RealizarPedido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RealizarPedido(string? direccionEntrega)
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

            // Validar nuevamente el stock antes de confirmar (por si cambió desde que se agregó al carrito)
            var productosIds = carrito.Select(c => c.ProductoId).ToList();
            var productos = await _context.Productos
                .Where(p => productosIds.Contains(p.Id))
                .ToListAsync();

            foreach (var item in carrito)
            {
                var producto = productos.FirstOrDefault(p => p.Id == item.ProductoId);
                if (producto == null || !producto.Estado)
                {
                    TempData["Error"] = $"El producto '{item.Nombre}' ya no está disponible.";
                    return RedirectToAction(nameof(Carrito));
                }

                if (item.Cantidad > producto.Stock)
                {
                    TempData["Error"] = $"Solo hay {producto.Stock} unidades disponibles de '{producto.Nombre}'. Por favor ajusta tu carrito.";
                    return RedirectToAction(nameof(Carrito));
                }
            }

            var pedido = new Pedido
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Fecha = DateTime.Now,
                Estado = "Pendiente",
                Total = carrito.Sum(c => c.Subtotal),
                DireccionEntrega = direccionEntrega?.Trim()
            };

            foreach (var item in carrito)
            {
                // Descontar stock del producto
                var producto = productos.First(p => p.Id == item.ProductoId);
                producto.Stock -= item.Cantidad;

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

            // Asignar automáticamente al repartidor disponible con menos pedidos activos
            var repartidorAsignado = await _context.Usuarios
                .Where(u => u.Rol == "Repartidor" && u.Estado)
                .GroupJoin(
                    _context.Pedidos.Where(p => p.Estado == "En camino" || p.Estado == "Confirmado"),
                    u => u.Uuid,
                    p => p.RepartidorId,
                    (u, pedidos) => new { Usuario = u, PedidosActivos = pedidos.Count() }
                )
                .OrderBy(x => x.PedidosActivos)
                .Select(x => x.Usuario)
                .FirstOrDefaultAsync();

            if (repartidorAsignado != null)
            {
                pedido.RepartidorId = repartidorAsignado.Uuid;
                pedido.Estado = "Confirmado";
            }

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Limpiar carrito
            GuardarCarrito(new List<CarritoItem>());

            var mensajeExito = repartidorAsignado != null
                ? $"Pedido realizado y asignado a {repartidorAsignado.Nombre} {repartidorAsignado.Apellido}."
                : "Pedido realizado. Se asignará un repartidor pronto.";

            TempData["Exito"] = mensajeExito;
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

        // â”€â”€ HELPERS SESIÃ“N â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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


