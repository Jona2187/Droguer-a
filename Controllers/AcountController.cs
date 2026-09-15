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
        // Admite peticiones AJAX (para no recargar ni desubicar al usuario) y peticiones normales.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string rolEsperado)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         Request.Headers["Accept"].ToString().Contains("application/json");

            var term = email?.Trim().ToLower() ?? "";

            // Buscar usuario por correo electrónico o por nombre
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == term || 
                                         u.Nombre.ToLower() == term || 
                                         (u.Nombre + " " + u.Apellido).ToLower() == term);

            // Verificar contraseña (con fallback a texto plano si la clave no era un hash BCrypt válido)
            bool esValido = false;
            if (usuario != null && !string.IsNullOrEmpty(usuario.Password))
            {
                try
                {
                    esValido = BCrypt.Net.BCrypt.Verify(password ?? "", usuario.Password);
                }
                catch
                {
                    esValido = (usuario.Password == password);
                }
            }

            // 1. Usuario no existe o contraseña incorrecta
            if (usuario == null || !esValido)
            {
                var msg = "Por favor, valide si el usuario o la contraseña son correctos.";
                if (isAjax)
                {
                    return Json(new { success = false, message = msg });
                }

                TempData["Error"] = msg;
                TempData["AbrirModal"] = ObtenerModal(rolEsperado);
                return RedirectToAction("Index", "Home");
            }

            // 2. Cuenta desactivada
            if (!usuario.Estado)
            {
                var msg = "Tu cuenta se encuentra desactivada. Contacta al equipo de soporte.";
                if (isAjax)
                {
                    return Json(new { success = false, message = msg });
                }

                TempData["Error"] = msg;
                TempData["AbrirModal"] = ObtenerModal(rolEsperado);
                return RedirectToAction("Index", "Home");
            }

            // Autenticación correcta
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

            var redirectUrl = usuario.Rol switch
            {
                "Administrador" => Url.Action("Dashboard", "Usuario"),
                "Empleado" => Url.Action("Productos", "Empleado"),
                _ => Url.Action("Catalogo", "Tienda")
            };

            if (isAjax)
            {
                return Json(new { success = true, redirectUrl });
            }

            return Redirect(redirectUrl ?? "/Home/Index");
        }

        // POST: /Account/Register  (registro público desde el modal de Cliente)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string nombre, string apellido, string email, string password)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         Request.Headers["Accept"].ToString().Contains("application/json");

            email = email?.Trim().ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                var msg = "Completa todos los campos para crear tu cuenta.";
                if (isAjax)
                {
                    return Json(new { success = false, message = msg });
                }

                TempData["Error"] = msg;
                TempData["AbrirModal"] = "_RegistroCliente";
                return RedirectToAction("Index", "Home");
            }

            var existe = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == email);
            if (existe)
            {
                var msg = $"El correo '{email}' ya se encuentra registrado.";
                if (isAjax)
                {
                    return Json(new { success = false, message = msg });
                }

                TempData["Error"] = msg;
                TempData["AbrirModal"] = "_RegistroCliente";
                return RedirectToAction("Index", "Home");
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

            var exitoMsg = "¡Cuenta creada con éxito! Ya puedes iniciar sesión.";
            if (isAjax)
            {
                return Json(new { success = true, message = exitoMsg, openModal = "_LoginCliente" });
            }

            TempData["Success"] = exitoMsg;
            TempData["AbrirModal"] = "_LoginCliente";
            return RedirectToAction("Index", "Home");
        }

        // GET y POST: /Account/Logout
        [HttpGet, HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            try
            {
                HttpContext.Session.Clear();
            }
            catch { }

            TempData["Success"] = "Sesión cerrada correctamente.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            TempData["Error"] = "No tienes permisos para acceder a esa sección.";
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Cliente"))
            {
                return RedirectToAction("Catalogo", "Tienda");
            }
            return RedirectToAction("Index", "Home");
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