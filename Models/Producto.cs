using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Drogueria.Models;

public class Producto
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres")]
    public string Nombre { get; set; } = "";

    [StringLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "La dosis o concentración es obligatoria")]
    [Range(1, 50000, ErrorMessage = "La dosis debe estar entre 1 y 50,000")]
    public int Miligramos { get; set; }

    [Required(ErrorMessage = "La unidad de medida es obligatoria")]
    [StringLength(20, ErrorMessage = "La unidad de medida no puede superar 20 caracteres")]
    public string UnidadMedida { get; set; } = "mg";

    [Required(ErrorMessage = "El precio es obligatorio")]
    [Column(TypeName = "decimal(10,2)")]
    [Range(0.01, 9999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal Precio { get; set; }

    // FK → Categoria
    [Required(ErrorMessage = "La categoría es obligatoria")]
    public Guid CategoriaId { get; set; }

    [ForeignKey(nameof(CategoriaId))]
    public Categoria? Categoria { get; set; }

    [Required(ErrorMessage = "El stock es obligatorio")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
    public int Stock { get; set; } = 0;

    [Required(ErrorMessage = "El límite de stock para alerta es obligatorio")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
    public int StockMinimo { get; set; } = 5;

    [Range(1, 1000, ErrorMessage = "El límite por pedido debe estar entre 1 y 1000 pastillas/unidades")]
    public int? LimiteMaximoPorPedido { get; set; }

    // Ruta relativa de la imagen: "uploads/productos/filename.jpg"
    [StringLength(300)]
    public string? Imagen { get; set; }

    public bool Estado { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}
