using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Drogueria.Models;

public class Usuario
{
    [Key]
    [StringLength(36)]
    public string Uuid { get; set; } = Guid.NewGuid().ToString();

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(25, ErrorMessage = "El nombre no puede superar 25 caracteres")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(25, ErrorMessage = "El apellido no puede superar 25 caracteres")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
    [DataType(DataType.EmailAddress)]
    [StringLength(60, ErrorMessage = "El correo no puede superar 60 caracteres")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    [StringLength(25, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 25 caracteres")]
    public string Password { get; set; } = "";

    public bool Estado { get; set; } = true;

    [Required(ErrorMessage = "El rol es obligatorio")]
    [StringLength(50)]
    public string Rol { get; set; } = "Cliente"; // Administrador, Empleado, Cliente

    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    public ICollection<Pedido> PedidosAsignados { get; set; } = new List<Pedido>();
    public ICollection<DireccionUsuario> Direcciones { get; set; } = new List<DireccionUsuario>();
}