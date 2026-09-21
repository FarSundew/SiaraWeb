using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "JefeGeneral,Administrador")]
    public class SubdirectorCarrerasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubdirectorCarrerasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Vista General: Listado de Carreras / Departamentos
        public async Task<IActionResult> Index()
        {
            var departamentos = await _context.Departamentos
                .Include(d => d.Subjects)
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View(departamentos);
        }

        // 2. Vista de Detalle: Muestra las Fechas de Corte y Asignaturas de ESA Carrera
        public async Task<IActionResult> Details(int id, int? periodoId)
        {
            var departamento = await _context.Departamentos
                .Include(d => d.Subjects!)
                    .ThenInclude(s => s.DocenteAsignaturas!)
                        .ThenInclude(da => da.Docente)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (departamento == null) return NotFound();

            var periodos = await _context.AcademicPeriods.OrderByDescending(p => p.Id).ToListAsync();
            int selectedPeriodId = periodoId ?? periodos.FirstOrDefault()?.Id ?? 0;

            // Busca las fechas de corte activas para esta carrera (propias o globales)
            var fechasCorte = await _context.CutoffDates
                .Where(c => c.AcademicPeriodId == selectedPeriodId &&
                           (c.DepartamentoId == id || c.DepartamentoId == null))
                .OrderBy(c => c.StartDate)
                .ToListAsync();

            ViewBag.Departamento = departamento;
            ViewBag.Periodos = periodos;
            ViewBag.PeriodoSeleccionado = selectedPeriodId;
            ViewBag.FechasCorte = fechasCorte;

            // Filtrar materias por el periodo seleccionado
            var materiasPeriodo = departamento.Subjects?
                .Where(s => s.AcademicPeriodId == selectedPeriodId)
                .ToList() ?? new List<Subject>();

            return View(materiasPeriodo);
        }
    }
}