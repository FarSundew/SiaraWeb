using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Models;
using SIARAWEB.ViewModels;
using SIARAWEB.Data; // <- Asegúrate de usar el namespace del DbContext
using System.Linq;
using System.Threading.Tasks;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class DocentesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context; // agregado

        public DocentesController(UserManager<ApplicationUser> userManager, ApplicationDbContext context) // modificado
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: Muestra la lista de docentes
        public async Task<IActionResult> Index()
        {
            // 1. Obtenemos a los usuarios
            var docentes = await _userManager.GetUsersInRoleAsync("Docente");
            var docentesIds = docentes.Select(d => d.Id).ToList();

            // 2. ⚠️ ESTA ES LA PARTE VITAL: Usamos .Include() para traer los datos del departamento
            var docentesConDepartamento = await _context.Users
                .Include(u => u.Departamento)
                .Where(u => docentesIds.Contains(u.Id))
                .ToListAsync();

            // 3. ⚠️ Enviamos "docentesConDepartamento" a la vista, NO la variable "docentes"
            return View(docentesConDepartamento);
        }

        // GET: Muestra el formulario para registrar
        public IActionResult Create()
        {
            // Poblar la lista de departamentos antes de mostrar la vista
            ViewBag.DepartamentosLista = new SelectList(_context.Departamentos, "Id", "Nombre");
            return View();
        }

        // POST: Recibe los datos y crea al docente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocenteViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name,
                    Rfc = model.Rfc,
                    Curp = model.Curp,
                    DepartamentoId = int.TryParse(model.Departamento, out var depId) ? depId : (int?)null
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Docente");
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.DepartamentosLista = new SelectList(_context.Departamentos, "Id", "Nombre", model.Departamento);
            return View(model);
        }

        // --- 4. EDITAR DOCENTE (GET) ---
        // --- 4. EDITAR DOCENTE (GET) ---
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Pasamos los datos del usuario a nuestro "molde" de edición
            var model = new DocenteEditViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Rfc = user.Rfc,    // Uso mayúsculas si tu modelo ApplicationUser lo tiene así
                Curp = user.Curp,  // Uso mayúsculas si tu modelo ApplicationUser lo tiene así

                // ⚠️ ERROR CORREGIDO: Antes decía user.Departamento
                DepartamentoId = user.DepartamentoId
            };

            // ⚠️ ERROR CORREGIDO: Antes decía user.Departamento
            ViewBag.DepartamentosLista = new SelectList(_context.Departamentos, "Id", "Nombre", user.DepartamentoId);

            return View(model);
        }

        // --- 5. EDITAR DOCENTE (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, DocenteEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) return NotFound();

                // Actualizamos los datos
                user.Name = model.Name;
                user.Email = model.Email;
                user.UserName = model.Email; // El UserName de Identity debe ser igual al Email
                user.Rfc = model.Rfc;
                user.Curp = model.Curp;

                // ⚠️ ERROR VITAL CORREGIDO: Antes decía user.Departamento = model.DepartamentoId;
                user.DepartamentoId = model.DepartamentoId;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Si hay error, recargamos los departamentos
            ViewBag.DepartamentosLista = new SelectList(_context.Departamentos, "Id", "Nombre", model.DepartamentoId);
            return View(model);
        }

        // --- 6. ELIMINAR DOCENTE (GET) ---
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user); // Mandamos el modelo directo de Identity
        }

        // --- 7. ELIMINAR DOCENTE (POST) ---
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Index));
        }


        public static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("docente"))
            {
                await roleManager.CreateAsync(new IdentityRole("docente"));
            }
        }





    }
}