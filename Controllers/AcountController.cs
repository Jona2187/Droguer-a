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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string rolEsperado)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         Request.Headers["Accept"].ToString().Contains("application/json");

            var term = email?.Trim().ToLower() ?? "";

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == term ||
                                         u.Nombre.ToLower() == term ||
                                         (u.Nombre + " " + u.Apellido).ToLower() == term);

            bool esValido = false;
            if (usuario != null && !string.IsNullOrEmpty(usuario.Password))
            {
                try { esValido = BCrypt.Net.BCrypt.Verify(password ?? "", usuario.Password); }
                catch { esValido = (usuario.Password == password); }
            }

            if (usuario == null || !esValido)
            {
                var msg = "Por favor, valide si el usuario o la contraseña son correctos.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                TempData["AbrirModal"] = ObtenerModal(rolEsperado);
                return RedirectToAction("Index", "Home");
            }

            if (!usuario.Estado)
            {
                var msg = "Tu cuenta se encuentra desactivada. Contacta al equipo de soporte.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                TempData["AbrirModal"] = ObtenerModal(rolEsperado);
                return RedirectToAction("Index", "Home");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Uuid),
                new Claim(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}".Trim()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

            TempData["Success"] = $"¡Bienvenido, {usuario.Nombre}!";

            var redirectUrl = usuario.Rol switch
            {
                "Administrador" => Url.Action("Dashboard", "Usuario"),
                "Empleado"      => Url.Action("Productos", "Empleado"),
                "Repartidor"    => Url.Action("MisDomicilios", "Repartidor"),
                _               => Url.Action("Catalogo", "Tienda")
            };

            if (isAjax) return Json(new { success = true, redirectUrl });
            return Redirect(redirectUrl ?? "/Home/Index");
        }

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
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg; TempData["AbrirModal"] = "_RegistroCliente";
                return RedirectToAction("Index", "Home");
            }

            if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == email))
            {
                var msg = $"El correo '{email}' ya se encuentra registrado.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg; TempData["AbrirModal"] = "_RegistroCliente";
                return RedirectToAction("Index", "Home");
            }

            _context.Usuarios.Add(new Usuario
            {
                Nombre = nombre.Trim(), Apellido = apellido.Trim(), Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(password), Rol = "Cliente", Estado = true
            });
            await _context.SaveChangesAsync();

            var exitoMsg = "¡Cuenta creada con éxito! Ya puedes iniciar sesión.";
            if (isAjax) return Json(new { success = true, message = exitoMsg, openModal = "_LoginCliente" });
            TempData["Success"] = exitoMsg; TempData["AbrirModal"] = "_LoginCliente";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            try { HttpContext.Session.Clear(); } catch { }
            TempData["Success"] = "Sesión cerrada correctamente.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            TempData["Error"] = "No tienes permisos para acceder a esta sección.";
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Cliente"))
                return RedirectToAction("Catalogo", "Tienda");
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Repartidor"))
                return RedirectToAction("MisDomicilios", "Repartidor");
            return RedirectToAction("Index", "Home");
        }

        private static string ObtenerModal(string? rolEsperado) => rolEsperado switch
        {
            "Administrador" => "_LoginAdmin",
            "Empleado"      => "_LoginEmpleado",
            "Repartidor"    => "_LoginRepartidor",
            "Cliente"       => "_LoginCliente",
            _               => "_LoginCliente"
        };
    }
}
