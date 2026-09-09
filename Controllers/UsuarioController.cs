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
                    return RedirectToAction(nameof(Index));
                }
            }

            if (ModelState.IsValid)
            {
                usuario.Uuid = Guid.NewGuid().ToString();
                usuario.Nombre = usuario.Nombre?.Trim() ?? "";
                usuario.Apellido = usuario.Apellido?.Trim() ?? "";
                usuario.Email = usuario.Email?.Trim().ToLower() ?? "";

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Usuario {usuario.Nombre} ({usuario.Rol}) creado con éxito.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Por favor verifica los campos obligatorios del formulario.";
            return RedirectToAction(nameof(Index));
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
                TempData["Success"] = $"Estado de {usuario.Nombre} actualizado.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Eliminar usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Usuario eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}