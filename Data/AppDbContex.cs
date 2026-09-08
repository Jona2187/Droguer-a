using Microsoft.EntityFrameworkCore;
using Drogueria.Models;

namespace Drogueria.Data;

public class AppDbContex : DbContext
{
    public AppDbContex(DbContextOptions<AppDbContex> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
}