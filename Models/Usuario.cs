using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Drogueria.Models;

public class Usuario
{
    [Key]
    [StringLength(36)]
    public string Uuid { get; set; } = Guid.NewGuid().ToString();

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(100)]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
    [DataType(DataType.EmailAddress)]
    [StringLength(150)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    [StringLength(255)]
    public string Password { get; set; } = "";

    public bool Estado { get; set; } = true;

    [Required(ErrorMessage = "El rol es obligatorio")]
    [StringLength(50)]
    public string Rol { get; set; } = "Cliente"; // Administrador, Empleado, Cliente
}