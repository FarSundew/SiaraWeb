using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data; // Aseg�rate de que este namespace coincida con el tuyo

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar la Cadena de Conexi�n a SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontr� la cadena de conexi�n 'DefaultConnection'.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configurar Data Protection (Guarda llaves criptogr�ficas en SQL Server)
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

// 3. Configurar Identity con reglas espec�ficas de SIARA
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Cambiar a true si usar�n confirmaci�n por correo

    // REQUISITO SIARA: Bloqueo de cuenta a los 3 intentos fallidos
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // Reglas de contrase�a
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// Agregar soporte para Controladores y Vistas
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configuraci�n del Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 4. Agregar Autenticaci�n al Pipeline (Debe ir antes de Authorization)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapear p�ginas de Identity (Login, Register, etc.)
app.MapRazorPages();

app.Run();