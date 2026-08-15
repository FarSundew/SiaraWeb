using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    // 🔒 SOLO EL JEFE GENERAL PUEDE GESTIONAR DEPARTAMENTOS
    [Authorize(Roles = "JefeGeneral")]
    public class DepartamentosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DepartamentosController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Departamentos
        public async Task<IActionResult> Index()
        {
            // Incluimos al Jefe de Carrera para mostrar su nombre en la tabla
            var departamentos = await _context.Departamentos
                                              .Include(d => d.HeadOfDepartment)
                                              .ToListAsync();
            return View(departamentos);
        }

        // GET: Departamentos/Create
        public async Task<IActionResult> Create()
        {
            // Buscar solo a los usuarios que tienen el rol de "JefeCarrera" para el SelectList
            var jefesDeCarrera = await _userManager.GetUsersInRoleAsync("JefeCarrera");
            ViewBag.Jefes = new SelectList(jefesDeCarrera, "Id", "FullName");

            return View();
        }

        // POST: Departamentos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Departamento departamento)
        {
            if (ModelState.IsValid)
            {
                _context.Add(departamento);
                await _context.SaveChangesAsync();
                TempData["Success"] = "El departamento fue registrado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            var jefesDeCarrera = await _userManager.GetUsersInRoleAsync("JefeCarrera");
            ViewBag.Jefes = new SelectList(jefesDeCarrera, "Id", "FullName", departamento.HeadOfDepartmentId);
            return View(departamento);
        }

        // POST: Departamentos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var departamento = await _context.Departamentos.FindAsync(id);
            if (departamento != null)
            {
                _context.Departamentos.Remove(departamento);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Departamento eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}