using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Drogueria.Models;

public class Pedido
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(36)]
    public string UsuarioId { get; set; } = "";

    [ForeignKey(nameof(UsuarioId))]
    public Usuario? Usuario { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    /// <summary>
    /// Pendiente, Confirmado, Entregado, Cancelado
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Estado { get; set; } = "Pendiente";

    public List<DetallePedido> Detalles { get; set; } = new();
}
