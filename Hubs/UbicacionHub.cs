using Microsoft.AspNetCore.SignalR;

namespace Drogueria.Hubs
{
    public class UbicacionHub : Hub
    {
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
    }
}
