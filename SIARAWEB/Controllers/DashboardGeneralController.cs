using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "JefeGeneral,Administrador")]
    public class DashboardGeneralController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardGeneralController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DashboardGeneral
        public async Task<IActionResult> Index(int? periodoId, int? departamentoId, string? faseSeleccionada)
        {
            // 1. Periodo activo por defecto
            var periodoActivo = await _context.AcademicPeriods
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            int activePeriodId = periodoId ?? periodoActivo?.Id ?? 0;
            string faseActual = faseSeleccionada ?? "Inicial";

            // 2. Catálogo de evidencias por cada fase académica
            var documentosPorFase = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Inicial", new List<string> { "Instrumentación Didáctica", "Instrumentos de Evaluación", "Prácticas de Laboratorio", "Proyecto Individual", "Evaluación Diagnóstica" } },
                { "Seguimiento1", new List<string> { "Avance (apart. 6)", "Calif. Parc. (Calificaciones Parciales)", "Instr. Eval. (Instrumentos de Evaluación)", "Eval. Diagn. (Evaluación Diagnóstica)", "Avance Proy. Ind. (Proyecto Individual)" } },
                { "Seguimiento2", new List<string> { "Avance Programático (apart. 6)", "Instrumentos de Evaluación", "Reporte de Seguimiento Intermedio" } },
                { "Final", new List<string> { "Acta de Calificaciones", "Instrumentos de Evaluación Finales", "Cierre de Proyecto / Reporte Final" } }
            };

            // 3. Consulta global filtrable
            var query = _context.Subjects
                .Include(s => s.Departamento)
                .Include(s => s.AcademicPeriod)
                .Include(s => s.DocenteAsignaturas!)
                    .ThenInclude(da => da.Docente)
                .Include(s => s.Documents!)
                    .ThenInclude(d => d.CutoffDate)
                .Where(s => s.AcademicPeriodId == activePeriodId)
                .AsQueryable();

            if (departamentoId.HasValue && departamentoId.Value > 0)
            {
                query = query.Where(s => s.DepartamentoId == departamentoId.Value);
            }

            var asignaturas = await query.OrderBy(s => s.Departamento!.Name).ThenBy(s => s.Name).ToListAsync();

            // 4. Catálogos para los selectores
            ViewBag.Periodos = await _context.AcademicPeriods.OrderByDescending(p => p.Id).ToListAsync();
            ViewBag.Departamentos = await _context.Departamentos.OrderBy(d => d.Name).ToListAsync();
            ViewBag.PeriodoSeleccionado = activePeriodId;
            ViewBag.DepartamentoSeleccionado = departamentoId;
            ViewBag.FaseActual = faseActual;
            ViewBag.DocumentosRequeridos = documentosPorFase.ContainsKey(faseActual) ? documentosPorFase[faseActual] : new List<string>();

            // 5. Métricas institucionales de cumplimiento para la fase actual
            var docsFaseActual = asignaturas.SelectMany(s => s.Documents ?? new List<Document>())
                .Where(d => d.CutoffDate?.PhaseType == faseActual || string.Equals(d.CutoffDate?.Name?.Trim(), faseActual.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            ViewBag.TotalMaterias = asignaturas.Count;
            ViewBag.TotalEntregasTiempo = docsFaseActual.Count(d => d.IsOnTime && d.FilePath != "N/A");
            ViewBag.TotalEntregasRetraso = docsFaseActual.Count(d => !d.IsOnTime && d.FilePath != "N/A");
            ViewBag.TotalNoAplica = docsFaseActual.Count(d => d.FilePath == "N/A");

            return View(asignaturas);
        }
    }
}