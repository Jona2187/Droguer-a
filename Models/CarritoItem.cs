namespace Drogueria.Models;

/// <summary>
/// Representa un ítem del carrito de compras (almacenado en sesión, no en BD).
/// </summary>
public class CarritoItem
{
    public Guid ProductoId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Imagen { get; set; }
    public decimal Precio { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal Subtotal => Precio * Cantidad;
}
