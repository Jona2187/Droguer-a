using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Drogueria.Data;
using Drogueria.Models;

namespace Drogueria.Controllers
{
    [Authorize(Roles = "Cliente,Administrador,Empleado,Repartidor")]
    public class DireccionController : Controller
    {
        private readonly AppDbContex _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public DireccionController(AppDbContex context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Direccion/Geocodificar?direccion=...&ciudad=...
        [HttpGet]
        public async Task<IActionResult> Geocodificar(string? direccion, string? ciudad)
        {
            if (string.IsNullOrWhiteSpace(direccion))
                return Json(new { success = false, message = "Ingresa una dirección primero." });

            // Limpiar caracteres que confunden a Nominatim (# en direcciones colombianas)
            var dirLimpia = (direccion ?? "")
                .Replace("#", "")
                .Replace("  ", " ")
                .Trim();
            var query = $"{dirLimpia} {ciudad} Colombia".Trim();
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=json&limit=1&addressdetails=1";

            try
            {
                var client = _httpClientFactory.CreateClient("Nominatim");
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return Json(new { success = false, message = "No se pudo consultar el servicio de mapas." });

                var json = await response.Content.ReadAsStringAsync();
                var resultados = JsonSerializer.Deserialize<JsonElement[]>(json);

                if (resultados == null || resultados.Length == 0)
                    return Json(new { success = false, message = "No se encontró la dirección. Verifica que sea correcta." });

                var r = resultados[0];
                var lat = double.Parse(r.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                var lon = double.Parse(r.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                var displayName = r.GetProperty("display_name").GetString() ?? query;

                return Json(new { success = true, lat, lon, displayName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al conectar con el servicio de mapas." });
            }
        }

        // GET: /Direccion/MisDirecciones
        [HttpGet]
        public async Task<IActionResult> MisDirecciones()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
                return Json(new { success = false, message = "No autenticado." });

            var direcciones = await _context.DireccionesUsuario
                .Where(d => d.UsuarioId == usuarioId)
                .OrderByDescending(d => d.EsPredeterminada)
                .ThenByDescending(d => d.FechaCreacion)
                .Select(d => new
                {
                    id = d.Id,
                    nombreContacto = d.NombreContacto,
                    telefono = d.Telefono,
                    direccion = d.Direccion,
                    ciudad = d.Ciudad,
                    departamento = d.Departamento,
                    esPredeterminada = d.EsPredeterminada,
                    fechaCreacion = d.FechaCreacion
                })
                .ToListAsync();

            return Json(new { success = true, data = direcciones });
        }

        // POST: /Direccion/Guardar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(
            Guid? id,
            string nombreContacto,
            string telefono,
            string direccion,
            string ciudad,
            string departamento,
            bool esPredeterminada = false)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
                return Json(new { success = false, message = "No autenticado." });

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(nombreContacto))
                return Json(new { success = false, message = "El nombre de contacto es obligatorio." });
            if (string.IsNullOrWhiteSpace(telefono))
                return Json(new { success = false, message = "El teléfono es obligatorio." });
            if (string.IsNullOrWhiteSpace(direccion))
                return Json(new { success = false, message = "La dirección es obligatoria." });
            if (string.IsNullOrWhiteSpace(ciudad))
                return Json(new { success = false, message = "La ciudad es obligatoria." });
            if (string.IsNullOrWhiteSpace(departamento))
                return Json(new { success = false, message = "El departamento es obligatorio." });

            bool esNueva = id == null || id == Guid.Empty;

            if (esNueva)
            {
                // Si es la primera dirección del usuario, ponerla como predeterminada automáticamente
                bool tieneDirecciones = await _context.DireccionesUsuario
                    .AnyAsync(d => d.UsuarioId == usuarioId);

                if (!tieneDirecciones)
                    esPredeterminada = true;

                // Si se marca como predeterminada, desmarcar las demás
                if (esPredeterminada)
                {
                    await _context.DireccionesUsuario
                        .Where(d => d.UsuarioId == usuarioId && d.EsPredeterminada)
                        .ExecuteUpdateAsync(s => s.SetProperty(d => d.EsPredeterminada, false));
                }

                var nueva = new DireccionUsuario
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = usuarioId,
                    NombreContacto = nombreContacto.Trim(),
                    Telefono = telefono.Trim(),
                    Direccion = direccion.Trim(),
                    Ciudad = ciudad.Trim(),
                    Departamento = departamento.Trim(),
                    EsPredeterminada = esPredeterminada,
                    FechaCreacion = DateTime.Now
                };

                _context.DireccionesUsuario.Add(nueva);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Dirección guardada correctamente.",
                    data = new
                    {
                        id = nueva.Id,
                        nombreContacto = nueva.NombreContacto,
                        telefono = nueva.Telefono,
                        direccion = nueva.Direccion,
                        ciudad = nueva.Ciudad,
                        departamento = nueva.Departamento,
                        esPredeterminada = nueva.EsPredeterminada,
                        fechaCreacion = nueva.FechaCreacion
                    }
                });
            }
            else
            {
                // Editar dirección existente
                var existente = await _context.DireccionesUsuario
                    .FirstOrDefaultAsync(d => d.Id == id && d.UsuarioId == usuarioId);

                if (existente == null)
                    return Json(new { success = false, message = "Dirección no encontrada." });

                // Si se marca como predeterminada, desmarcar las demás
                if (esPredeterminada && !existente.EsPredeterminada)
                {
                    await _context.DireccionesUsuario
                        .Where(d => d.UsuarioId == usuarioId && d.EsPredeterminada)
                        .ExecuteUpdateAsync(s => s.SetProperty(d => d.EsPredeterminada, false));
                }

                existente.NombreContacto = nombreContacto.Trim();
                existente.Telefono = telefono.Trim();
                existente.Direccion = direccion.Trim();
                existente.Ciudad = ciudad.Trim();
                existente.Departamento = departamento.Trim();
                existente.EsPredeterminada = esPredeterminada;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Dirección actualizada correctamente.",
                    data = new
                    {
                        id = existente.Id,
                        nombreContacto = existente.NombreContacto,
                        telefono = existente.Telefono,
                        direccion = existente.Direccion,
                        ciudad = existente.Ciudad,
                        departamento = existente.Departamento,
                        esPredeterminada = existente.EsPredeterminada,
                        fechaCreacion = existente.FechaCreacion
                    }
                });
            }
        }

        // POST: /Direccion/Eliminar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
                return Json(new { success = false, message = "No autenticado." });

            var direccion = await _context.DireccionesUsuario
                .FirstOrDefaultAsync(d => d.Id == id && d.UsuarioId == usuarioId);

            if (direccion == null)
                return Json(new { success = false, message = "Dirección no encontrada." });

            bool eraPredeterminada = direccion.EsPredeterminada;
            _context.DireccionesUsuario.Remove(direccion);
            await _context.SaveChangesAsync();

            // Si era predeterminada, asignar la más reciente como predeterminada
            if (eraPredeterminada)
            {
                var siguiente = await _context.DireccionesUsuario
                    .Where(d => d.UsuarioId == usuarioId)
                    .OrderByDescending(d => d.FechaCreacion)
                    .FirstOrDefaultAsync();

                if (siguiente != null)
                {
                    siguiente.EsPredeterminada = true;
                    await _context.SaveChangesAsync();
                }
            }

            // Retornar la lista actualizada
            var listaDirs = await _context.DireccionesUsuario
                .Where(d => d.UsuarioId == usuarioId)
                .OrderByDescending(d => d.EsPredeterminada)
                .ThenByDescending(d => d.FechaCreacion)
                .Select(d => new
                {
                    id = d.Id,
                    nombreContacto = d.NombreContacto,
                    telefono = d.Telefono,
                    direccion = d.Direccion,
                    ciudad = d.Ciudad,
                    departamento = d.Departamento,
                    esPredeterminada = d.EsPredeterminada
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                message = "Dirección eliminada correctamente.",
                data = listaDirs
            });
        }

        // POST: /Direccion/Predeterminar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Predeterminar(Guid id)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioId))
                return Json(new { success = false, message = "No autenticado." });

            var direccion = await _context.DireccionesUsuario
                .FirstOrDefaultAsync(d => d.Id == id && d.UsuarioId == usuarioId);

            if (direccion == null)
                return Json(new { success = false, message = "Dirección no encontrada." });

            // Desmarcar todas las del usuario
            await _context.DireccionesUsuario
                .Where(d => d.UsuarioId == usuarioId && d.EsPredeterminada)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.EsPredeterminada, false));

            // Marcar la seleccionada
            direccion.EsPredeterminada = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Dirección predeterminada actualizada." });
        }
    }
}
