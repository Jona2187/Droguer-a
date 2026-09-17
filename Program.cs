using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using Drogueria.Data;
using Drogueria.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("Nominatim", client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org");
    client.DefaultRequestHeaders.Add("User-Agent", "DrogueriaApp/1.0 (contacto@drogueria.com)");
    client.DefaultRequestHeaders.Add("Accept-Language", "es");
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
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
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

app.Run();



