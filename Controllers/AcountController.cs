using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Drogueria.Data;
using Drogueria.Models;

namespace Drogueria.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContex _context;

        public AccountController(AppDbContex context)
        {
            _context = context;
        }

        // POST: /Account/Login
        // Lo usan los 3 modales (Empleado, Administrador, Cliente) del Index.
        // "rolEsperado" viaja como campo oculto en cada formulario para que
        // nadie entre por el modal de Cliente con una cuenta de Administrador, etc.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string rolEsperado)
        {
            email = email?.Trim().ToLower() ?? "";

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            // Usuario no existe o la contraseña no coincide con el hash guardado
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(password ?? "", usuario.Password))
            {
                TempData["Error"] = "Correo o contraseña incorrectos.";
                TempData["AbrirModal"] = ObtenerModal(rolEsperado);
                return RedirectToAction("Pagina_Inicio", "Home");
            }

            // Cuenta desactivada (borrado lógico desde el panel de Usuarios)
            if (!usuario.Estado)
            {
                TempData["Error"] = "Tu cuenta está desactivada. Contacta a un administrador.";
                TempData["AbrirModal"] = ObtenerModal(rolEsperado);
                return RedirectToAction("Pagina_Inicio", "Home");
            }

            // El rol real del usuario no corresponde al modal por el que entró
            if (!string.IsNullOrWhiteSpace(rolEsperado) &&
                !string.Equals(usuario.Rol, rolEsperado, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = $"Este acceso es exclusivo para el rol '{rolEsperado}'.";
                TempData["AbrirModal"] = ObtenerModal(rolEsperado);
                return RedirectToAction("Pagina_Inicio", "Home");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Uuid),
                new Claim(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}".Trim()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            TempData["Success"] = $"¡Bienvenido, {usuario.Nombre}!";

            // Redirige según el rol del usuario que acaba de entrar
            return usuario.Rol switch
            {
                "Administrador" => RedirectToAction("Index", "Usuario"),
                "Empleado" => RedirectToAction("Index", "Usuario"),
                _ => RedirectToAction("Pagina_Inicio", "Home")
            };
        }

        // POST: /Account/Register  (registro público desde el modal de Cliente)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string nombre, string apellido, string email, string password)
        {
            email = email?.Trim().ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Completa todos los campos para crear tu cuenta.";
                TempData["AbrirModal"] = "_RegistroCliente";
                return RedirectToAction("Pagina_Inicio", "Home");
            }

            var existe = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == email);
            if (existe)
            {
                TempData["Error"] = $"El correo '{email}' ya está registrado.";
                TempData["AbrirModal"] = "_RegistroCliente";
                return RedirectToAction("Pagina_Inicio", "Home");
            }

            var usuario = new Usuario
            {
                Nombre = nombre.Trim(),
                Apellido = apellido.Trim(),
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Rol = "Cliente",
                Estado = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            TempData["Success"] = "¡Cuenta creada con éxito! Ya puedes iniciar sesión.";
            TempData["AbrirModal"] = "_LoginCliente";
            return RedirectToAction("Pagina_Inicio", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Sesión cerrada correctamente.";
            return RedirectToAction("Pagina_Inicio", "Home");
        }

        // Decide qué modal reabrir en el Index cuando el login/registro falla
        private static string ObtenerModal(string? rolEsperado) => rolEsperado switch
        {
            "Administrador" => "_LoginAdmin",
            "Empleado" => "_LoginEmpleado",
            "Cliente" => "_LoginCliente",
            _ => "_LoginCliente"
        };
    }
}