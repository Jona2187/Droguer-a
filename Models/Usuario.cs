using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Drogueria.Models;

public class Usuario
{
    [Key]
    [Column(TypeName = "varchar(36)")]
    public string Uuid { get; set; } = Guid.NewGuid().ToString();

    public string Nombre { get; set; } = "";

    public string Apellido { get; set; } = "";

    public string Email { get; set; } = "";

    public string Password { get; set; } = "";

    public bool Estado { get; set; }
}