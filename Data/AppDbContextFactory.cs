using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Drogueria.Data;

/// <summary>
/// Permite que "dotnet ef migrations add" cree el DbContext en design-time
/// sin necesitar una conexión MySQL activa.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContex>
{
    public AppDbContex CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContex>();

        // Cadena de conexión usada SOLO en design-time (generación de migraciones)
        var connectionString = "Server=localhost;Port=3306;Database=Drogueria;User=root;Password=;";
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));

        return new AppDbContex(optionsBuilder.Options);
    }
}
