using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using Drogueria.Data;
using Drogueria.Hubs;
using Drogueria.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<AsignacionRepartidorService>();

builder.Services.AddHttpClient("Nominatim", client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org");
    client.DefaultRequestHeaders.Add("User-Agent", "DrogueriaApp/1.0 (contacto@drogueria.com)");
    client.DefaultRequestHeaders.Add("Accept-Language", "es");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("OSRM", client =>
{
    client.BaseAddress = new Uri("https://router.project-osrm.org");
    client.DefaultRequestHeaders.Add("User-Agent", "DrogueriaApp/1.0 (contacto@drogueria.com)");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddSignalR();


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Sesión para el carrito de compras
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<AppDbContex>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 30)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()
    )
);

// Fijar cultura invariante para que los inputs numéricos con punto decimal
// (como los inputs HTML type="number") se bindeen correctamente.
var supportedCultures = new[] { CultureInfo.InvariantCulture };
builder.Services.Configure<RequestLocalizationOptions>(opts =>
{
    opts.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(CultureInfo.InvariantCulture);
    opts.SupportedCultures = supportedCultures;
    opts.SupportedUICultures = supportedCultures;
});

var app = builder.Build();
    
app.UseStatusCodePagesWithReExecute("/Home/ErrorStatus", "?code={0}");

app.UseRequestLocalization();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapHub<UbicacionHub>("/hubs/ubicacion");

// Asegurar columnas de perfil y vehículo en la tabla Usuarios sin generar errores en consola
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContex>();
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            conn.Open();
        }

        void AsegurarColumna(string tabla, string columna, string tipoDef)
        {
            using var cmdCheck = conn.CreateCommand();
            cmdCheck.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                  AND TABLE_NAME = @tabla 
                  AND COLUMN_NAME = @columna;";

            var pTabla = cmdCheck.CreateParameter();
            pTabla.ParameterName = "@tabla";
            pTabla.Value = tabla;
            cmdCheck.Parameters.Add(pTabla);

            var pCol = cmdCheck.CreateParameter();
            pCol.ParameterName = "@columna";
            pCol.Value = columna;
            cmdCheck.Parameters.Add(pCol);

            var count = Convert.ToInt32(cmdCheck.ExecuteScalar());
            if (count == 0)
            {
                using var cmdAlter = conn.CreateCommand();
                cmdAlter.CommandText = $"ALTER TABLE `{tabla}` ADD COLUMN `{columna}` {tipoDef};";
                cmdAlter.ExecuteNonQuery();
            }
        }

        // Asegurar columnas en Usuarios
        AsegurarColumna("Usuarios", "Telefono", "VARCHAR(20) NULL");
        AsegurarColumna("Usuarios", "TipoVehiculo", "VARCHAR(50) NULL");
        AsegurarColumna("Usuarios", "Placa", "VARCHAR(20) NULL");
        AsegurarColumna("Usuarios", "EstaEnLinea", "TINYINT(1) NOT NULL DEFAULT 1");

        // Asegurar columnas en Pedidos (evita 'Unknown column p.CodigoConfirmacion')
        AsegurarColumna("Pedidos", "CodigoConfirmacion", "VARCHAR(20) NULL");
        AsegurarColumna("Pedidos", "MetodoPago", "VARCHAR(50) NULL");
        AsegurarColumna("Pedidos", "DireccionEntrega", "VARCHAR(300) NULL");
        AsegurarColumna("Pedidos", "LatitudRepartidor", "DOUBLE NULL");
        AsegurarColumna("Pedidos", "LongitudRepartidor", "DOUBLE NULL");
        AsegurarColumna("Pedidos", "UltimaUbicacionFecha", "DATETIME(6) NULL");

        // Ampliar campo de descripción de productos a 2000 caracteres
        try
        {
            using var cmdDesc = conn.CreateCommand();
            cmdDesc.CommandText = "ALTER TABLE Productos MODIFY COLUMN Descripcion VARCHAR(2000) NULL;";
            cmdDesc.ExecuteNonQuery();
        }
        catch { }

        // Asegurar tabla persistente de Mensajes de Chat
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS MensajesChat (
                    Id CHAR(36) NOT NULL PRIMARY KEY,
                    PedidoId CHAR(36) NOT NULL,
                    Remitente VARCHAR(150) NOT NULL,
                    Mensaje TEXT NOT NULL,
                    Fecha DATETIME NOT NULL,
                    INDEX idx_chat_pedido (PedidoId)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            cmd.ExecuteNonQuery();
        }
    }
    catch { }
}

app.Run();



