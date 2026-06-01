using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class DocenteAsignaturasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DocenteAsignaturasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DocenteAsignaturas (Lista de asignaciones)
        public async Task<IActionResult> Index()
        {
            // Traemos la lista incluyendo los datos del Docente y la Asignatura
            var asignaciones = await _context.DocenteAsignaturas
                .Include(d => d.Docente)
                .Include(d => d.Subject)
                .ToListAsync();

            return View(asignaciones);
        }

        // GET: DocenteAsignaturas/Create (Pantalla para asignar)
        public IActionResult Create()
        {
            // Enviamos a la vista las opciones para los menús desplegables
            ViewData["DocenteId"] = new SelectList(_context.Users, "Id", "Name");
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Name");
            return View();
        }

        // POST: DocenteAsignaturas/Create (Guardar asignación)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DocenteId,SubjectId")] DocenteAsignatura docenteAsignatura)
        {
            // 1. Le decimos a ASP.NET Core que ignore los objetos completos, ya que solo guardaremos los puros IDs
            ModelState.Remove("Docente");
            ModelState.Remove("Subject");

            // 2. Ahora sí, la validación pasará correctamente
            if (ModelState.IsValid)
            {
                _context.Add(docenteAsignatura);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si algo más falla, regresamos las listas desplegables a la vista
            ViewData["DocenteId"] = new SelectList(_context.Users, "Id", "Name", docenteAsignatura.DocenteId);
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Name", docenteAsignatura.SubjectId);

            return View(docenteAsignatura);
        }
    }
}