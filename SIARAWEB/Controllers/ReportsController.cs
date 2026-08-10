using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // ⚠️ Necesario para SelectList de la gráfica
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;
using SIARAWEB.ViewModels; // ⚠️ Necesario para ReporteFiltroViewModel
using System.Linq;
using System.Threading.Tasks;

namespace SIARAWEB.Controllers
{
    // 🔒 BLOQUEO: Solo los Jefes de Carrera pueden ver las gráficas y reportes
    [Authorize(Roles = "Administrador")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1️⃣ NUEVO: Pantalla principal que lista a los maestros
        public async Task<IActionResult> Index()
        {
            // Traemos a todos los usuarios que tienen rol de Docente
            var docentes = await _userManager.GetUsersInRoleAsync("Docente");

            // Rellenamos ViewBag.Departamentos para que la vista pueda resolver Nombre por DepartamentoId
            ViewBag.Departamentos = await _context.Departamentos.ToListAsync();

            return View(docentes);
        }

        // 2️⃣ NUEVO: Genera el documento oficial del docente seleccionado (Puro texto, listo para PDF)
        public async Task<IActionResult> DocenteReport(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // Buscamos al maestro por su ID de Identity
            var docente = await _userManager.FindByIdAsync(id);
            if (docente == null) return NotFound();

            // Pasamos el nombre del maestro a la vista
            //ViewBag.NombreDocente = docente.Name ?? docente.UserName;

            // Rellenar el nombre del departamento del docente (si existe)
            /*string departamentoNombre = "N/A";
            if (docente.DepartamentoId.HasValue)
            {
                var departamento = await _context.Departamentos.FindAsync(docente.DepartamentoId.Value);
                departamentoNombre = departamento?.Nombre ?? "N/A";
            }
            ViewBag.DepartamentoDocente = departamentoNombre;*/

            // Buscamos a qué materias está asignado este maestro específico
            var asignaturasIds = await _context.DocenteAsignaturas
                .Where(da => da.DocenteId == id)
                .Select(da => da.SubjectId)
                .ToListAsync();

            // Extraemos los seguimientos de esas materias
            var seguimientos = await _context.AcademicTrackings
                .Include(a => a.Subject)
                .Where(a => asignaturasIds.Contains(a.SubjectId))
                .OrderBy(a => a.Subject.Name).ThenBy(a => a.UnitNumber)
                .ToListAsync();

            return View(seguimientos);
        }

        // ====================================================================
        // 👇 MÉTODOS DEL MÓDULO DE GRÁFICAS Y FILTROS INTERACTIVOS 👇
        // ====================================================================

        // 3️⃣ GET: Pantalla del Generador de Gráficas (Antes era Index, ahora es Graficas)
        public async Task<IActionResult> Graficas()
        {
            if (_context.Departamentos != null)
            {
                ViewBag.Departamentos = new SelectList(await _context.Departamentos.ToListAsync(), "Id", "Nombre");
            }
            else
            {
                ViewBag.Departamentos = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            return View(new ReporteFiltroViewModel());
        }

        // 4️⃣ POST (AJAX): Devuelve los datos para dibujar la gráfica en vivo con Chart.js
        [HttpPost]
        public IActionResult ObtenerDatosGrafica([FromBody] ReporteFiltroViewModel filtros)
        {
            var datosSimulados = new
            {
                Aprobacion = 48.3,
                Reprobacion = 25.3,
                Desercion = 16.5
            };

            return Json(datosSimulados);
        }
    }
}