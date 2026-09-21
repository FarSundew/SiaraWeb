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
            // Incluye el departamento de adscripción base
            var users = await _userManager.Users
                .Include(u => u.Departamento)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(users);
        }

        // GET: ApplicationUsers/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = new SelectList(await _roleManager.Roles.Select(r => r.Name).ToListAsync());
            ViewBag.Departamentos = new SelectList(await _context.Departamentos.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");

            return View();
        }

        // POST: ApplicationUsers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser user, string role, string password)
        {
            user.UserName = user.Email;
            user.EmailConfirmed = true;

            // Limpieza de validaciones de campos fuera del modelo directo
            ModelState.Remove("password");
            ModelState.Remove("role");

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrWhiteSpace(user.RFC))
                    user.RFC = user.RFC.Trim().ToUpper();

                if (!string.IsNullOrWhiteSpace(user.CURP))
                    user.CURP = user.CURP.Trim().ToUpper();

                // 1. Crear el usuario con la contraseña indicada
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // 2. Asignarle el rol seleccionado
                    if (!string.IsNullOrEmpty(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }

                    TempData["Success"] = $"Usuario {user.FullName} registrado con éxito.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new SelectList(await _roleManager.Roles.Select(r => r.Name).ToListAsync(), role);
            ViewBag.Departamentos = new SelectList(await _context.Departamentos.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", user.DepartamentoId);

            return View(user);
        }

        // GET: ApplicationUsers/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.Users
                .Include(u => u.Departamento)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var currentRole = userRoles.FirstOrDefault();

            ViewBag.Roles = new SelectList(await _roleManager.Roles.Select(r => r.Name).ToListAsync(), currentRole);
            ViewBag.Departamentos = new SelectList(await _context.Departamentos.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", user.DepartamentoId);

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
                // 1. Actualizar datos personales y adscripción base
                user.FullName = model.FullName;
                user.RFC = model.RFC?.Trim().ToUpper();
                user.CURP = model.CURP?.Trim().ToUpper();
                user.Email = model.Email;
                user.UserName = model.Email;
                user.DepartamentoId = model.DepartamentoId; // 🟢 Guarda el departamento seleccionado

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

            ViewBag.Roles = new SelectList(await _roleManager.Roles.Select(r => r.Name).ToListAsync(), role);
            ViewBag.Departamentos = new SelectList(await _context.Departamentos.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", model.DepartamentoId);

            return View(model);
        }

        // POST: ApplicationUsers/LockAccount (Inhabilitación lógica respetando historial)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockAccount(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                await _userManager.UpdateAsync(user);
                TempData["Success"] = $"La cuenta de {user.FullName} ha sido bloqueada.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: ApplicationUsers/UnlockAccount (Reactivación de cuenta)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAccount(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = true;
                user.LockoutEnd = null;
                await _userManager.ResetAccessFailedCountAsync(user);

                await _userManager.UpdateAsync(user);
                TempData["Success"] = $"La cuenta de {user.FullName} ha sido desbloqueada exitosamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}