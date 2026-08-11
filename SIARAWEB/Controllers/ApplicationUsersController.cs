using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    // 🔒 SOLO EL ADMINISTRADOR PUEDE ENTRAR A ESTE MÓDULO
    [Authorize(Roles = "Administrador")]
    public class ApplicationUsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public ApplicationUsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: ApplicationUsers (Lista de todos los usuarios)
        public async Task<IActionResult> Index()
        {
            // Solo mostramos usuarios activos (opcional, puedes mostrar todos)
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // GET: ApplicationUsers/Create
        public IActionResult Create()
        {
            // Llenamos un ViewBag con los roles disponibles para el select (dropdown)
            ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name).ToList());
            return View();
        }

        // POST: ApplicationUsers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser user, string role, string password)
        {
            // Forzamos campos obligatorios que Identity requiere
            user.UserName = user.Email;
            user.EmailConfirmed = true; // Para que puedan entrar directamente

            // Limpiamos los errores del ModelState de campos que no aplican aquí
            ModelState.Remove("password");
            ModelState.Remove("role");

            if (ModelState.IsValid)
            {
                // 1. Crear el usuario con la contraseña elegida
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // 2. Asignarle el rol seleccionado
                    if (!string.IsNullOrEmpty(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }

                    TempData["Success"] = "Usuario creado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                // Si falla (ej. contraseña muy débil o correo duplicado), mostramos errores
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name).ToList(), role);
            return View(user);
        }

        // POST: ApplicationUsers/LockAccount (Para "eliminar" o bloquear a un maestro sin borrar su historial)
        [HttpPost]
        public async Task<IActionResult> LockAccount(string id)
        {

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = false; // Desactivación lógica
                user.LockoutEnd = DateTimeOffset.MaxValue; // Bloqueo de acceso de Identity
                await _userManager.UpdateAsync(user);
                TempData["Success"] = $"La cuenta de {user.FullName} ha sido bloqueada.";
            }
            return RedirectToAction(nameof(Index));
        }
        // POST: ApplicationUsers/UnlockAccount (Para desbloquear la cuenta de un usuario)
        [HttpPost]
        public async Task<IActionResult> UnlockAccount(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = true;                           // Reactivación lógica
                user.LockoutEnd = null;                          // Elimina la fecha de bloqueo en Identity
                await _userManager.ResetAccessFailedCountAsync(user); // Reinicia los intentos fallidos de contraseña

                await _userManager.UpdateAsync(user);
                TempData["Success"] = $"La cuenta de {user.FullName} ha sido desbloqueada exitosamente.";
            }
            return RedirectToAction(nameof(Index));
        }
        // GET: ApplicationUsers/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Obtener el rol actual del usuario
            var userRoles = await _userManager.GetRolesAsync(user);
            var currentRole = userRoles.FirstOrDefault();

            ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name).ToList(), currentRole);

            return View(user);
        }

        // POST: ApplicationUsers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser model, string role)
        {
            if (id != model.Id) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                // 1. Actualizar datos personales
                user.FullName = model.FullName;
                user.RFC = model.RFC?.ToUpper();
                user.CURP = model.CURP?.ToUpper();
                user.Email = model.Email;
                user.UserName = model.Email;

                var updateResult = await _userManager.UpdateAsync(user);

                if (updateResult.Succeeded)
                {
                    // 2. Actualizar Rol si cambió
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                    if (!string.IsNullOrEmpty(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }

                    TempData["Success"] = $"Los datos de {user.FullName} han sido actualizados.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name).ToList(), role);
            return View(model);
        }
    }
}