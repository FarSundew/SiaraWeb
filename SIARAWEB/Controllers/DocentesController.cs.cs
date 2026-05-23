using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SIARAWEB.Models;
using SIARAWEB.ViewModel;
using SIARAWEB.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace SIARAWEB.Controllers
{
    //[Authorize(Roles = "admin")]
    public class DocentesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DocentesController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Muestra la lista de docentes
        public IActionResult Index()
        {
            // Filtramos a los usuarios (podrías filtrar después solo los que tienen rol 'docente')
            var docentes = _userManager.Users.ToList();
            return View(docentes);
        }

        // GET: Muestra el formulario para registrar
        public IActionResult Create()
        {
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
                    Curp = model.Curp,
                    Rfc = model.Rfc
                };

                // CreateAsync guarda el usuario y encripta su contraseña automáticamente
                var result = await _userManager.CreateAsync(user, model.Password!);

                if (result.Succeeded)
                {
                    // Le asignamos el rol de docente
                    await _userManager.AddToRoleAsync(user, "docente");
                    return RedirectToAction(nameof(Index));
                }

                // Si la contraseña no cumple las reglas (ej. no tiene mayúsculas), muestra el error
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: Docentes/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            // Buscamos al usuario en la base de datos
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Pasamos los datos actuales al formulario
            var model = new DocenteEditViewModel
            {
                Id = user.Id,
                Name = user.Name ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Curp = user.Curp ?? string.Empty,
                Rfc = user.Rfc ?? string.Empty
            };

            return View(model);
        }

        // POST: Docentes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DocenteEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null) return NotFound();

                // Actualizamos las propiedades con lo que viene del formulario
                user.Name = model.Name;
                user.Email = model.Email;
                user.UserName = model.Email; // En Identity, el UserName suele ser igual al Email
                user.Curp = model.Curp;
                user.Rfc = model.Rfc;

                // Guardamos los cambios de forma segura
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index)); // Regresa a la tabla si todo sale bien
                }

                // Si hay errores (ej. correo duplicado), los mostramos
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

    }
}