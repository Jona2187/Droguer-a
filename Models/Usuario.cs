using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Drogueria.Models;

public class Usuario
{
    [Key]
    [StringLength(36)]
    public string Uuid { get; set; } = Guid.NewGuid().ToString();

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar 100 caracteres")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "El correo electrónico debe ser válido, contener '@', un dominio y una extensión como .com o .co")]
    [DataType(DataType.EmailAddress)]
    [StringLength(150, ErrorMessage = "El correo no puede superar 150 caracteres")]
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