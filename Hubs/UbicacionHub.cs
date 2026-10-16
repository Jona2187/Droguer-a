using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Drogueria.Data;
using Drogueria.Models;

namespace Drogueria.Hubs
{
    public class UbicacionHub : Hub
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // Historial en memoria de respaldo rápido durante la sesión activa
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, List<object>> _historialChatMemoria = new();

        public UbicacionHub(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Permite que un cliente (o repartidor) se una al grupo del pedido para escuchar/transmitir actualizaciones de ubicación.
        /// </summary>
        public async Task JoinOrderGroup(string pedidoId)
        {
            if (!string.IsNullOrWhiteSpace(pedidoId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, pedidoId);
            }
        }

        /// <summary>
        /// Transmite la ubicación GPS actual del repartidor a todos los clientes suscritos al pedido.
        /// </summary>
        public async Task EnviarUbicacion(string pedidoId, double lat, double lng)
        {
            if (!string.IsNullOrWhiteSpace(pedidoId))
            {
                await Clients.Group(pedidoId).SendAsync("ReceiveLocation", pedidoId, lat, lng);
            }
        }

        /// <summary>
        /// Transmite la ubicación GPS del repartidor junto con tiempo estimado y distancia calculados.
        /// </summary>
        public async Task EnviarUbicacionConRuta(string pedidoId, double lat, double lng, string? tiempoEstimado, string? distancia)
        {
            if (!string.IsNullOrWhiteSpace(pedidoId))
            {
                await Clients.Group(pedidoId).SendAsync("ReceiveLocationWithRoute", pedidoId, lat, lng, tiempoEstimado, distancia);
                await Clients.Group(pedidoId).SendAsync("ReceiveLocation", pedidoId, lat, lng);
            }
        }

        /// <summary>
        /// Obtiene el historial permanente de mensajes de un pedido desde la base de datos (y memoria si aplica).
        /// </summary>
        public async Task<List<object>> ObtenerHistorialChat(string pedidoId)
        {
            if (string.IsNullOrWhiteSpace(pedidoId))
                return new List<object>();

            var resultado = new List<object>();

            try
            {
                if (Guid.TryParse(pedidoId, out var pId))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContex>();

                    var dbMensajes = await db.MensajesChat
                        .AsNoTracking()
                        .Where(m => m.PedidoId == pId)
                        .OrderBy(m => m.Fecha)
                        .Select(m => new
                        {
                            remitente = m.Remitente,
                            mensaje = m.Mensaje,
                            hora = m.Fecha.ToString("HH:mm")
                        })
                        .ToListAsync();

                    if (dbMensajes.Any())
                    {
                        return dbMensajes.Cast<object>().ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al consultar historial de chat en BD: {ex.Message}");
            }

            // Fallback a memoria si la BD aún no tenía o falló
            if (_historialChatMemoria.TryGetValue(pedidoId, out var memoriaList))
            {
                lock (memoriaList)
                {
                    return new List<object>(memoriaList);
                }
            }

            return resultado;
        }

        /// <summary>
        /// Envía un mensaje de chat entre cliente y repartidor para un pedido específico,
        /// guardándolo de forma PERMANENTE en la base de datos y transmitiéndolo por SignalR.
        /// </summary>
        public async Task EnviarMensajeChat(string pedidoId, string remitente, string mensaje)
        {
            if (!string.IsNullOrWhiteSpace(pedidoId) && !string.IsNullOrWhiteSpace(mensaje))
            {
                var ahora = DateTime.Now;
                var horaTexto = ahora.ToString("HH:mm");
                var msgObj = new { remitente, mensaje, hora = horaTexto };

                // 1. Guardar en memoria para acceso ultrarrápido
                var lista = _historialChatMemoria.GetOrAdd(pedidoId, _ => new List<object>());
                lock (lista)
                {
                    lista.Add(msgObj);
                }

                // 2. Guardar en Base de Datos de manera PERMANENTE
                try
                {
                    if (Guid.TryParse(pedidoId, out var pId))
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContex>();

                        var entidad = new MensajeChat
                        {
                            Id = Guid.NewGuid(),
                            PedidoId = pId,
                            Remitente = remitente,
                            Mensaje = mensaje.Trim(),
                            Fecha = ahora
                        };

                        db.MensajesChat.Add(entidad);
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error guardando mensaje en base de datos: {ex.Message}");
                }

                // 3. Notificar en tiempo real a todos los suscritos (Cliente, Repartidor, Administrador)
                await Clients.Group(pedidoId).SendAsync("RecibirMensajeChat", pedidoId, remitente, mensaje, horaTexto);
            }
        }
    }
}
