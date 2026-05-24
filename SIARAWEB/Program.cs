using Microsoft.AspNetCore.DataProtection;
using SIARAWEB.Data; // Aseg�rate de que este namespace coincida con el tuyo
using SIARAWEB.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// 1. Configurar la Cadena de Conexi�n a SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontr� la cadena de conexi�n 'DefaultConnection'.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configurar Data Protection (Guarda llaves criptogr�ficas en SQL Server)
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

// 3. Configurar Identity con reglas espec�ficas de SIARA, Vistas por defecto y Roles
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
.AddRoles<IdentityRole>() // <-- ESTO HABILITA LOS ROLES PARA PODER ASIGNAR EL "DOCENTE"
.AddEntityFrameworkStores<ApplicationDbContext>();

// Agregar soporte para Controladores y Vistas
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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


// 5. DATA SEEDING: Creaci�n de Roles y Administrador Maestro
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Crear el rol "admin"
    if (!await roleManager.RoleExistsAsync("admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("admin"));
    }

    // Crear el rol "DOCENTE" para que no d� el error al registrar un maestro
    if (!await roleManager.RoleExistsAsync("DOCENTE"))
    {
        await roleManager.CreateAsync(new IdentityRole("DOCENTE"));
    }

    // Crear el usuario Administrador Maestro por defecto
    string emailAdmin = "admin@siara.edu.mx";
    if (await userManager.FindByEmailAsync(emailAdmin) == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = emailAdmin,
            Email = emailAdmin,
            Name = "Jefe de Carrera",
            Curp = "ADMIN0000000000000",
            Rfc = "ADMIN000000"
        };

        // Guarda al usuario con su contrase�a
        var result = await userManager.CreateAsync(adminUser, "AdminSiara.2026");

        if (result.Succeeded)
        {
            // Le asignamos el rol de administrador
            await userManager.AddToRoleAsync(adminUser, "admin");
        }
    }
}

app.Run();