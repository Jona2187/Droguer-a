using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Drogueria.Data;
using Drogueria.Models;

namespace Drogueria.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly AppDbContex _context;

        public UsuarioController(AppDbContex context)
        {
            _context = context;
        }

        // Dashboard Administrador (Métricas, accesos rápidos y últimos pedidos)
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalProductos = await _context.Productos.CountAsync();
            ViewBag.TotalUsuarios = await _context.Usuarios.CountAsync();
            ViewBag.TotalCategorias = await _context.Categorias.CountAsync();
            ViewBag.TotalPedidos = await _context.Pedidos.CountAsync();
            ViewBag.IngresosTotales = await _context.Pedidos.Where(p => p.Estado != "Cancelado").SumAsync(p => (decimal?)p.Total) ?? 0m;

            ViewBag.PedidosPendientes = await _context.Pedidos.CountAsync(p => p.Estado == "Pendiente");
            ViewBag.PedidosConfirmados = await _context.Pedidos.CountAsync(p => p.Estado == "Confirmado");
            ViewBag.PedidosEntregados = await _context.Pedidos.CountAsync(p => p.Estado == "Entregado");
            ViewBag.PedidosCancelados = await _context.Pedidos.CountAsync(p => p.Estado == "Cancelado");

            ViewBag.ProductosStockBajo = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Stock <= 10)
                .OrderBy(p => p.Stock)
                .Take(5)
                .ToListAsync();

            var ultimosPedidos = await _context.Pedidos
                .Include(p => p.Usuario)
                .OrderByDescending(p => p.Fecha)
                .Take(6)
                .ToListAsync();

            return View("~/Views/Home/pages/_Dashboard.cshtml", ultimosPedidos);
        }

        // Listar usuarios
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios.OrderByDescending(u => u.Uuid).ToListAsync();
            return View("~/Views/Home/pages/_Usuarios.cshtml", usuarios);
        }

        // Crear usuario (POST desde el Modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario)
        {
            // Validar que el rol sea uno de los permitidos: Empleado, Administrador, Cliente
            var rolNormalizado = usuario.Rol?.Trim();
            if (string.Equals(rolNormalizado, "Administrador", StringComparison.OrdinalIgnoreCase))
                usuario.Rol = "Administrador";
            else if (string.Equals(rolNormalizado, "Cliente", StringComparison.OrdinalIgnoreCase))
                usuario.Rol = "Cliente";
            else
                usuario.Rol = "Empleado";

            // Validar correo duplicado
            if (!string.IsNullOrWhiteSpace(usuario.Email))
            {
                var emailExiste = await _context.Usuarios
                    .AnyAsync(u => u.Email.ToLower() == usuario.Email.Trim().ToLower());

                if (emailExiste)
                {
                    TempData["Error"] = $"El correo '{usuario.Email}' ya está registrado con otro usuario.";
                    return Redirect("/Usuario");
                }
            }

            if (ModelState.IsValid)
            {
                usuario.Uuid = Guid.NewGuid().ToString();
                usuario.Nombre = usuario.Nombre?.Trim() ?? "";
                usuario.Apellido = usuario.Apellido?.Trim() ?? "";
                usuario.Email = usuario.Email?.Trim().ToLower() ?? "";
                usuario.Password = BCrypt.Net.BCrypt.HashPassword(usuario.Password?.Trim() ?? "");

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Usuario ({usuario.Rol}) {usuario.Nombre} creado con éxito.";
                return Redirect("/Usuario");
            }

            TempData["Error"] = "Por favor verifica los campos obligatorios del formulario.";
            return Redirect("/Usuario");
        }

        // Alternar Estado Activo / Inactivo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEstado(string id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                usuario.Estado = !usuario.Estado;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Estado de {usuario.Nombre} actualizado correctamente.";
            }
            else
            {
                TempData["Error"] = "Usuario no encontrado.";
            }

            return RedirectToAction(nameof(Index));
        }

        // "Eliminar" usuario -> en realidad solo se desactiva (borrado lógico)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                usuario.Estado = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Usuario {usuario.Nombre} desactivado correctamente.";
            }
            else
            {
                TempData["Error"] = "Usuario no encontrado.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Usuario/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Usuario usuario)
        {
            // Si el usuario no escribió una contraseña nueva, no la exigimos ni la tocamos
            var nuevaPassword = usuario.Password?.Trim();
            if (string.IsNullOrWhiteSpace(nuevaPassword))
            {
                ModelState.Remove(nameof(Usuario.Password));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Verifica los campos obligatorios.";
                return RedirectToAction(nameof(Index));
            }

            var existente = await _context.Usuarios.FindAsync(usuario.Uuid);
            if (existente == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Validar correo duplicado (excluyendo al propio usuario que se está editando)
            if (!string.IsNullOrWhiteSpace(usuario.Email))
            {
                var emailExiste = await _context.Usuarios
                    .AnyAsync(u => u.Uuid != usuario.Uuid && u.Email.ToLower() == usuario.Email.Trim().ToLower());

                if (emailExiste)
                {
                    TempData["Error"] = $"El correo '{usuario.Email}' ya está registrado con otro usuario.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Normalizar y asignar los datos
            existente.Nombre   = usuario.Nombre?.Trim() ?? "";
            existente.Apellido = usuario.Apellido?.Trim() ?? "";
            existente.Email    = usuario.Email?.Trim().ToLower() ?? "";
            existente.Estado   = usuario.Estado;

            // Solo actualizamos la contraseña si el usuario escribió una nueva
            if (!string.IsNullOrWhiteSpace(nuevaPassword))
            {
                existente.Password = BCrypt.Net.BCrypt.HashPassword(nuevaPassword);
            }

            // Normalizar rol (igual que en Create)
            var rol = usuario.Rol?.Trim();
            if (string.Equals(rol, "Administrador", StringComparison.OrdinalIgnoreCase))
                existente.Rol = "Administrador";
            else if (string.Equals(rol, "Cliente", StringComparison.OrdinalIgnoreCase))
                existente.Rol = "Cliente";
            else
                existente.Rol = "Empleado";

            // Guardar cambios
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Usuario {existente.Nombre} actualizado con éxito.";
            return RedirectToAction(nameof(Index));
        }

    }
}