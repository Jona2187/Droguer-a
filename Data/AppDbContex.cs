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
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Producto> Productos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Categoria — PK como char(36) UUID en MySQL
        modelBuilder.Entity<Categoria>()
            .Property(c => c.Id)
            .HasColumnType("char(36)")
            .ValueGeneratedNever();

        // Producto — PK como char(36) UUID en MySQL
        modelBuilder.Entity<Producto>()
            .Property(p => p.Id)
            .HasColumnType("char(36)")
            .ValueGeneratedNever();

        // Producto → Categoria: FK char(36), sin delete en cascada
        modelBuilder.Entity<Producto>()
            .Property(p => p.CategoriaId)
            .HasColumnType("char(36)");

        modelBuilder.Entity<Producto>()
            .HasOne(p => p.Categoria)
            .WithMany()
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}