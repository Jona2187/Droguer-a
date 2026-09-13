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

    [Required(ErrorMessage = "El precio es obligatorio")]
    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 9999999.99, ErrorMessage = "El precio debe ser mayor o igual a 0")]
    public decimal Precio { get; set; }

    // FK → Categoria
    [Required(ErrorMessage = "La categoría es obligatoria")]
    public Guid CategoriaId { get; set; }

    [ForeignKey(nameof(CategoriaId))]
    public Categoria? Categoria { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
    public int Stock { get; set; } = 0;

    // Ruta relativa de la imagen: "uploads/productos/filename.jpg"
    [StringLength(300)]
    public string? Imagen { get; set; }

    public bool Estado { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}
