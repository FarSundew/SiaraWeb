using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // ⚠️ Necesario para buscar a los docentes
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models; // ⚠️ Necesario para ApplicationUser y AcademicTracking
using System.Linq;
using System.Threading.Tasks;

namespace SIARAWEB.Controllers
{
    // 🔒 BLOQUEO: Solo los Jefes de Carrera pueden ver las gráficas y reportes [1]
    [Authorize(Roles = "Administrador")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // 1️⃣ AÑADIMOS EL ADMINISTRADOR DE USUARIOS
        private readonly UserManager<ApplicationUser> _userManager;

        // 2️⃣ ACTUALIZAMOS EL CONSTRUCTOR PARA RECIBIR AMBOS SERVICIOS
        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 3️⃣ NUEVO: Pantalla principal que lista a los maestros
        public async Task<IActionResult> Index()
        {
            // Traemos a todos los usuarios que tienen rol de Docente
            var docentes = await _userManager.GetUsersInRoleAsync("Docente");

            // Rellenamos ViewBag.Departamentos para que la vista pueda resolver Nombre por DepartamentoId
            ViewBag.Departamentos = await _context.Departamentos.ToListAsync();

            return View(docentes);
        }

        // 4️⃣ NUEVO: Genera el documento oficial del docente seleccionado (Puro texto, listo para PDF)
        public async Task<IActionResult> DocenteReport(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // Buscamos al maestro por su ID de Identity
            var docente = await _userManager.FindByIdAsync(id);
            if (docente == null) return NotFound();

            // Pasamos el nombre del maestro a la vista
            ViewBag.NombreDocente = docente.Name ?? docente.UserName;

            // Rellenar el nombre del departamento del docente (si existe)
            string departamentoNombre = "N/A";
            if (docente.DepartamentoId.HasValue)
            {
                var departamento = await _context.Departamentos.FindAsync(docente.DepartamentoId.Value);
                departamentoNombre = departamento?.Nombre ?? "N/A";
            }
            ViewBag.DepartamentoDocente = departamentoNombre;

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

        // 👇 ¡ATENCIÓN! 👇
        // DEBAJO DE ESTA LÍNEA PEGA O MANTÉN EL MÉTODO QUE YA TENÍAS PARA TU GRÁFICA DE PASTEL.
        // Ejemplo: public IActionResult GraficaDesempeno() { ... }
    }
}