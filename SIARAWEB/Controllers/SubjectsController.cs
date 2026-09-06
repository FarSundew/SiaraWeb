using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]
    public class SubjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Subjects
        public async Task<IActionResult> Index()
        {
            // Reemplaza la línea problemática en el método Index por la siguiente:
            var asignaturas = await _context.Subjects
                .Include(s => s.Departamento)
                .Include(s => s.AcademicPeriod)
                .Include(s => s.DocenteAsignaturas!)
                    .ThenInclude(da => da.Docente)
                .ToListAsync();
            return View(asignaturas);
        }

        // GET: Subjects/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.AcademicPeriods = new SelectList(_context.AcademicPeriods.Where(p => p.IsActive), "Id", "Name");
            ViewBag.Departamentos = new SelectList(_context.Departamentos, "Id", "Name");

            // Obtenemos los usuarios con rol de Docente para mostrarlos en la vista
            // 1. Traemos a los de los tres roles
            var docentesPuros = await _userManager.GetUsersInRoleAsync("Docente");
            var jefesCarrera = await _userManager.GetUsersInRoleAsync("JefeCarrera");
            var jefesGenerales = await _userManager.GetUsersInRoleAsync("JefeGeneral");

            // 2. Unimos todas las listas y quitamos los duplicados (por si alguien tiene dos roles a la vez)
            var todosLosProfesores = docentesPuros.Union(jefesCarrera).Union(jefesGenerales)
                                                  .DistinctBy(u => u.Id)
                                                  .OrderBy(u => u.FullName)
                                                  .ToList();

            // 3. Lo mandamos a la vista
            ViewBag.Docentes = new SelectList(todosLosProfesores, "Id", "FullName");

            return View();
        }

        // POST: Subjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Subject subject, List<string>? selectedDocentes)
        {
            if (ModelState.IsValid)
            {
                // 1. Guardamos la asignatura primero para que genere su ID
                _context.Add(subject);
                await _context.SaveChangesAsync();

                // 2. Si el Jefe de Carrera seleccionó docentes, los vinculamos en la tabla intermedia "DocenteAsignatura"
                if (selectedDocentes != null && selectedDocentes.Any())
                {
                    foreach (var docenteId in selectedDocentes)
                    {
                        var asignacion = new DocenteAsignatura
                        {
                            SubjectId = subject.Id,
                            DocenteId = docenteId
                        };
                        _context.DocenteAsignaturas.Add(asignacion);
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Asignatura registrada y docentes vinculados exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AcademicPeriods = new SelectList(_context.AcademicPeriods.Where(p => p.IsActive), "Id", "Name", subject.AcademicPeriodId);
            ViewBag.Departamentos = new SelectList(_context.Departamentos, "Id", "Name", subject.DepartamentoId);
            // 1. Traemos a los de los tres roles
            var docentesPuros = await _userManager.GetUsersInRoleAsync("Docente");
            var jefesCarrera = await _userManager.GetUsersInRoleAsync("JefeCarrera");
            var jefesGenerales = await _userManager.GetUsersInRoleAsync("JefeGeneral");

            // 2. Unimos todas las listas y quitamos los duplicados (por si alguien tiene dos roles a la vez)
            var todosLosProfesores = docentesPuros.Union(jefesCarrera).Union(jefesGenerales)
                                                  .DistinctBy(u => u.Id)
                                                  .OrderBy(u => u.FullName)
                                                  .ToList();

            // 3. Lo mandamos a la vista
            ViewBag.Docentes = new SelectList(todosLosProfesores, "Id", "FullName");

            return View(subject);
        }

        // POST: Subjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subject = await _context.Subjects
                                        .Include(s => s.DocenteAsignaturas)
                                        .FirstOrDefaultAsync(s => s.Id == id);
            if (subject != null)
            {
                // Eliminamos primero las relaciones en la tabla intermedia si existen
                if (subject.DocenteAsignaturas != null && subject.DocenteAsignaturas.Any())
                {
                    _context.DocenteAsignaturas.RemoveRange(subject.DocenteAsignaturas);
                }

                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Asignatura eliminada correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}