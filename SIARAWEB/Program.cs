using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data; // Asegúrate de que este namespace coincida con el tuyo
using SIARAWEB.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar la Cadena de Conexión a SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configurar Data Protection (Guarda llaves criptográficas en SQL Server)
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

// 3. Configurar Identity con reglas específicas de SIARA y habilitación de Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Cambiar a true si usarán confirmación por correo

    // REQUISITO SIARA: Bloqueo de cuenta a los 3 intentos fallidos
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // Reglas de contraseña
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
})
.AddRoles<IdentityRole>() // Habilita la gestión de roles
.AddEntityFrameworkStores<ApplicationDbContext>();

// Agregar soporte para Controladores y Vistas (Exigiendo Inicio de Sesión Global)
builder.Services.AddControllersWithViews(options =>
{
    var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
});

// Agregar Razor Pages y PERMITIR el acceso libre a la carpeta de cuentas (Login, Recuperar Contraseña, etc.)
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToAreaFolder("Identity", "/Account");
});

var app = builder.Build();

// Configuración del Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 4. Agregar Autenticación y Autorización al Pipeline
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapear páginas de Identity (Login, Register, etc.)
app.MapRazorPages();


// 5. DATA SEEDING: Creación automática de los 4 Roles y Administrador Maestro
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // 5.1 Crear los 4 roles jerárquicos del sistema si no existen
        string[] rolesDelSistema = { "Administrador", "JefeGeneral", "JefeCarrera", "Docente" };

        foreach (var rol in rolesDelSistema)
        {
            var rolExiste = await roleManager.RoleExistsAsync(rol);
            if (!rolExiste)
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        // 5.2 Crear la cuenta del Administrador Maestro por defecto
        var adminEmail = "admin@tecnm.mx";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var nuevoAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Administrador del Sistema",
                IsActive = true
            };

            // Creamos al usuario con una contraseña inicial segura
            var result = await userManager.CreateAsync(nuevoAdmin, "AdminSiara2026!");

            if (result.Succeeded)
            {
                // Le asignamos el rol con todos los privilegios técnicos
                await userManager.AddToRoleAsync(nuevoAdmin, "Administrador");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al sembrar los roles y el administrador.");
    }
}

app.Run();