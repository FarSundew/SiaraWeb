using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? periodoId, int? departamentoId, string? search)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // 1. Definir Periodo
            var periodoActivo = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive)
                                ?? await _context.AcademicPeriods.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
            int activePeriodId = periodoId ?? periodoActivo?.Id ?? 0;

            // 2. Consulta Base de Asignaturas
            var subjectQuery = _context.Subjects
                .Include(s => s.Departamento)
                .Include(s => s.DocenteAsignaturas!.Where(da => da.AcademicPeriodId == activePeriodId)).ThenInclude(da => da.Docente)
                .Include(s => s.Documents!.Where(d => d.DocumentTask != null && d.DocumentTask.AcademicPeriodId == activePeriodId)).ThenInclude(d => d.DocumentTask)
                .Include(s => s.AcademicTrackings!.Where(t => t.DocumentTask != null && t.DocumentTask.AcademicPeriodId == activePeriodId)).ThenInclude(t => t.DocumentTask)
                .Where(s => s.DocenteAsignaturas!.Any(da => da.AcademicPeriodId == activePeriodId))
                .AsQueryable();

            // 3. Seguridad y Filtros
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                subjectQuery = subjectQuery.Where(s => s.DepartamentoId == currentUser.DepartamentoId);
            }
            else if (departamentoId.HasValue && departamentoId.Value > 0)
            {
                subjectQuery = subjectQuery.Where(s => s.DepartamentoId == departamentoId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string cleanSearch = search.Trim().ToLower();
                subjectQuery = subjectQuery.Where(s =>
                    s.Name.ToLower().Contains(cleanSearch) ||
                    s.Code.ToLower().Contains(cleanSearch) ||
                    s.DocenteAsignaturas.Any(da => da.Docente != null && da.Docente.FullName.ToLower().Contains(cleanSearch)));
            }

            var asignaturas = await subjectQuery.OrderBy(s => s.Name).ToListAsync();

            // 4. Armar el Semáforo
            var listaSemaforo = new List<SemaforoRow>();
            foreach (var materia in asignaturas)
            {
                var item = new SemaforoRow
                {
                    SubjectId = materia.Id,
                    SubjectCode = materia.Code,
                    SubjectName = materia.Name,
                    DocenteName = materia.DocenteAsignaturas?.FirstOrDefault()?.Docente?.FullName ?? "Sin asignar"
                };

                string EvaluarSemaforo(string phaseType)
                {
                    var docsFase = materia.Documents?.Where(d => d.DocumentTask?.PhaseType == phaseType).ToList();
                    var trackingsFase = materia.AcademicTrackings?.Where(t => t.DocumentTask?.PhaseType == phaseType).ToList();
                    bool hasDocs = docsFase != null && docsFase.Any();
                    bool hasTrackings = trackingsFase != null && trackingsFase.Any();
                    if (!hasDocs && !hasTrackings) return "bg-secondary";
                    return hasDocs && docsFase.Any(d => !d.IsOnTime) ? "bg-danger" : "bg-success";
                }

                item.ColorInicial = EvaluarSemaforo("Inicial");
                item.ColorSeg1 = EvaluarSemaforo("Seguimiento1");
                item.ColorSeg2 = EvaluarSemaforo("Seguimiento2");
                item.ColorFinal = EvaluarSemaforo("Final");
                listaSemaforo.Add(item);
            }

            // 5. Armar la Sábana
            var subjectIds = asignaturas.Select(s => s.Id).ToList();
            var allTrackings = await _context.AcademicTrackings
                .Include(t => t.DocumentTask)
                .Where(t => subjectIds.Contains(t.SubjectId) && t.DocumentTask != null && t.DocumentTask.AcademicPeriodId == activePeriodId)
                .OrderBy(t => t.Subject!.Name).ThenBy(t => t.DocumentTaskId).ThenBy(t => t.UnitNumber)
                .ToListAsync();

            var listaSabana = allTrackings.Select(t => new SabanaRow
            {
                TrackingId = t.Id,
                SubjectId = t.SubjectId,
                SubjectCode = asignaturas.FirstOrDefault(s => s.Id == t.SubjectId)?.Code ?? "-",
                SubjectName = asignaturas.FirstOrDefault(s => s.Id == t.SubjectId)?.Name ?? "-",
                TeacherName = asignaturas.FirstOrDefault(s => s.Id == t.SubjectId)?.DocenteAsignaturas?.FirstOrDefault()?.Docente?.FullName ?? "Sin Asignar",
                PhaseName = t.DocumentTask?.Title ?? "Seguimiento",
                UnitNumber = t.UnitNumber,
                ApprovalPercentage = t.ApprovalPercentage,
                FailurePercentage = t.FailurePercentage,
                ApprovalStatus = t.ApprovalStatus,
                CorrectiveAction = t.Observations
            }).ToList();

            // 6. Completar el ViewModel Unificado
            var vm = new AnalyticsDashboardViewModel
            {
                ActivePeriodId = activePeriodId,
                SelectedDepartamentoId = departamentoId,
                SearchQuery = search,
                AcademicPeriods = await _context.AcademicPeriods.OrderByDescending(p => p.Id).ToListAsync(),
                Departamentos = await _context.Departamentos.OrderBy(d => d.Name).ToListAsync(),
                
                TotalAsignaturas = asignaturas.Count,
                DocentesEvaluados = asignaturas.SelectMany(s => s.DocenteAsignaturas!).Select(da => da.DocenteId).Distinct().Count(),
                PromedioAprobacion = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.ApprovalPercentage), 1) : 0,
                PromedioReprobacion = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.FailurePercentage), 1) : 0,
                AlertasRiesgo = listaSabana.Count(s => s.IsHighRisk),

                SemaforoRows = listaSemaforo,
                EntregasATiempo = listaSemaforo.Count(s => s.ColorInicial == "bg-success" || s.ColorSeg1 == "bg-success" || s.ColorSeg2 == "bg-success"),
                EntregasDesfasadas = listaSemaforo.Count(s => s.ColorInicial == "bg-danger" || s.ColorSeg1 == "bg-danger" || s.ColorSeg2 == "bg-danger"),
                EntregasPendientes = listaSemaforo.Count(s => s.ColorInicial == "bg-secondary" && s.ColorSeg1 == "bg-secondary" && s.ColorSeg2 == "bg-secondary"),
                
                SabanaRows = listaSabana
            };

            return View(vm);
        }

        // GET: Reports/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var materia = await _context.Subjects
                .Include(s => s.Departamento)
                .Include(s => s.DocenteAsignaturas!).ThenInclude(da => da.Docente)
                .Include(s => s.Documents!).ThenInclude(d => d.DocumentTask)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (materia == null) return NotFound();

            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                if (materia.DepartamentoId != currentUser?.DepartamentoId) return Forbid();
            }

            return View(materia);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera")]
        public async Task<IActionResult> ReviewDocument(int documentId, int subjectId, string? feedback, string status)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var document = await _context.Documents.Include(d => d.Subject).FirstOrDefaultAsync(d => d.Id == documentId);
            if (document != null)
            {
                if (document.Subject?.DepartamentoId != currentUser?.DepartamentoId && !User.IsInRole("Administrador")) return Forbid();
                document.ApprovalStatus = status;
                document.Feedback = feedback ?? string.Empty;
                _context.Documents.Update(document);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"El documento fue {status.ToUpper()} con éxito.";
            }
            return RedirectToAction(nameof(Details), new { id = subjectId });
        }
    }
}

