using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Drogueria.Models;

public class DireccionUsuario
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(36)]
    public string UsuarioId { get; set; } = "";

    [ForeignKey(nameof(UsuarioId))]
    public Usuario? Usuario { get; set; }

    [Required(ErrorMessage = "El nombre de contacto es obligatorio")]
    [StringLength(100)]
    public string NombreContacto { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [StringLength(20)]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "La dirección es obligatoria")]
    [StringLength(300)]
    public string Direccion { get; set; } = "";

    [Required(ErrorMessage = "La ciudad es obligatoria")]
    [StringLength(100)]
    public string Ciudad { get; set; } = "";

    [Required(ErrorMessage = "El departamento es obligatorio")]
    [StringLength(100)]
    public string Departamento { get; set; } = "";

    public bool EsPredeterminada { get; set; } = false;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}
