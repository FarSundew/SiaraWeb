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

        // GET: Reports (Semáforo de Cumplimiento Docente)
        public async Task<IActionResult> Index(int? periodoId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // 1. Determinar el periodo seleccionado o el activo por defecto
            var periodoActivo = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive)
                                ?? await _context.AcademicPeriods.OrderByDescending(p => p.Id).FirstOrDefaultAsync();

            int activePeriodId = periodoId ?? periodoActivo?.Id ?? 0;

            // 2. Consulta base filtrada por periodo
            var query = _context.Subjects
                .Include(s => s.Departamento)
                .Include(s => s.AcademicPeriod)
                .Include(s => s.DocenteAsignaturas!).ThenInclude(da => da.Docente)
                .Include(s => s.Documents!).ThenInclude(d => d.CutoffDate)
                .Where(s => s.AcademicPeriodId == activePeriodId)
                .AsQueryable();

            // 🔒 REGLA MULTI-TENANT: Si es Jefe de Carrera, se filtra estrictamente por SU departamento
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                query = query.Where(s => s.DepartamentoId == currentUser.DepartamentoId);
            }

            var asignaturas = await query.OrderBy(s => s.Name).ToListAsync();

            var listaSemaforo = new List<SemaforoViewModel>();

            foreach (var materia in asignaturas)
            {
                var item = new SemaforoViewModel
                {
                    SubjectId = materia.Id,
                    SubjectCode = materia.Code,
                    SubjectName = materia.Name,
                    DocenteName = materia.DocenteAsignaturas?.FirstOrDefault()?.Docente?.FullName ?? "Sin asignar"
                };

                // Función local para evaluar el color del semáforo por cada fase
                string EvaluarSemaforo(string phaseType)
                {
                    var docsFase = materia.Documents?.Where(d => d.CutoffDate?.PhaseType == phaseType).ToList();

                    if (docsFase == null || !docsFase.Any()) return "bg-secondary"; // Gris: Pendiente

                    // Si al menos un documento se entregó con retraso, se pinta rojo
                    bool tieneAtraso = docsFase.Any(d => !d.IsOnTime);
                    return tieneAtraso ? "bg-danger" : "bg-success";
                }

                item.ColorInicial = EvaluarSemaforo("Inicial");
                item.ColorSeg1 = EvaluarSemaforo("Seguimiento1");
                item.ColorSeg2 = EvaluarSemaforo("Seguimiento2");
                item.ColorFinal = EvaluarSemaforo("Final");

                listaSemaforo.Add(item);
            }

            // 3. Estadísticas para Gráficas: Calculadas ÚNICAMENTE sobre las materias y periodo en pantalla
            var subjectIds = asignaturas.Select(s => s.Id).ToList();
            var allTrackings = await _context.AcademicTrackings
                .Where(t => subjectIds.Contains(t.SubjectId))
                .ToListAsync();

            ViewBag.Aprobacion = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.ApprovalPercentage), 1) : 0;
            ViewBag.Reprobacion = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.FailurePercentage), 1) : 0;
            ViewBag.Desercion = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.DropoutPercentage), 1) : 0;

            ViewBag.DocsATiempo = listaSemaforo.Count(s => s.ColorInicial == "bg-success");
            ViewBag.DocsDesfasados = listaSemaforo.Count(s => s.ColorInicial == "bg-danger");
            ViewBag.DocsPendientes = listaSemaforo.Count(s => s.ColorInicial == "bg-secondary");

            ViewBag.Periodos = await _context.AcademicPeriods.OrderByDescending(p => p.Id).ToListAsync();
            ViewBag.PeriodoSeleccionado = activePeriodId;

            return View(listaSemaforo);
        }

        // GET: Reports/Details/5?faseSeleccionada=Inicial
        public async Task<IActionResult> Details(int id, string? faseSeleccionada)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var materia = await _context.Subjects
                .Include(s => s.AcademicPeriod)
                .Include(s => s.Departamento)
                .Include(s => s.DocenteAsignaturas!)
                    .ThenInclude(da => da.Docente)
                .Include(s => s.Documents!)
                    .ThenInclude(d => d.CutoffDate)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (materia == null) return NotFound();

            // 🔒 Validación de seguridad cruzada
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                if (materia.DepartamentoId != currentUser?.DepartamentoId)
                {
                    return Forbid(); // No puede auditar materias de otra carrera
                }
            }

            string faseActual = faseSeleccionada ?? "Inicial";

            var documentosPorFase = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "Inicial", new List<string> {
                        "Instrumentación Didáctica",
                        "Instrumentos de Evaluación",
                        "Prácticas de Laboratorio",
                        "Proyecto Individual",
                        "Evaluación Diagnóstica"
                    }
                },
                {
                    "Seguimiento1", new List<string> {
                        "Avance (apart. 6)",
                        "Calif. Parc. (Calificaciones Parciales)",
                        "Instr. Eval. (Instrumentos de Evaluación)",
                        "Eval. Diagn. (Evaluación Diagnóstica)",
                        "Avance Proy. Ind. (Proyecto Individual)"
                    }
                },
                {
                    "Seguimiento2", new List<string> {
                        "Avance Programático (apart. 6)",
                        "Instrumentos de Evaluación",
                        "Reporte de Seguimiento Intermedio"
                    }
                },
                {
                    "Final", new List<string> {
                        "Acta de Calificaciones",
                        "Instrumentos de Evaluación Finales",
                        "Cierre de Proyecto / Reporte Final"
                    }
                }
            };

            ViewBag.FaseActual = faseActual;
            ViewBag.DocumentosRequeridos = documentosPorFase.ContainsKey(faseActual)
                ? documentosPorFase[faseActual]
                : new List<string>();

            return View(materia);
        }

        // POST: Reports/ReviewDocument (Aprobar o Rechazar con Feedback)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera")] // 🔒 Exclusivo del Jefe de Carrera
        public async Task<IActionResult> ReviewDocument(int documentId, int subjectId, string? feedback, string status)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var document = await _context.Documents
                .Include(d => d.Subject)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document != null)
            {
                // 🔒 Seguridad: Verificar que el documento pertenezca a la carrera del usuario
                if (document.Subject?.DepartamentoId != currentUser?.DepartamentoId && !User.IsInRole("Administrador"))
                {
                    return Forbid();
                }

                document.ApprovalStatus = status;
                document.Feedback = feedback ?? string.Empty;

                _context.Documents.Update(document);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"El documento fue {status.ToUpper()} con éxito.";
            }
            else
            {
                TempData["Error"] = "Hubo un problema al encontrar el documento.";
            }

            return RedirectToAction(nameof(Details), new { id = subjectId });
        }

        // GET: Reports/GeneralReport (Sábana General de Calificaciones)
        public async Task<IActionResult> GeneralReport(int? periodId, int? cutoffDateId, string? search, bool onlyHighRisk = false)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var currentPeriod = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive)
                                ?? await _context.AcademicPeriods.OrderByDescending(p => p.Id).FirstOrDefaultAsync();

            int activePeriodId = periodId ?? currentPeriod?.Id ?? 0;

            var query = _context.AcademicTrackings
                .Include(t => t.Subject)
                    .ThenInclude(s => s!.DocenteAsignaturas!)
                        .ThenInclude(da => da.Docente)
                .Include(t => t.Subject)
                    .ThenInclude(s => s!.Departamento)
                .Include(t => t.CutoffDate)
                .Where(t => t.Subject != null && t.Subject.AcademicPeriodId == activePeriodId)
                .AsQueryable();

            // 🔒 REGLA MULTI-TENANT: El Jefe de Carrera solo ve su propio departamento en la sábana
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                query = query.Where(t => t.Subject!.DepartamentoId == currentUser.DepartamentoId);
            }

            if (cutoffDateId.HasValue && cutoffDateId.Value > 0)
            {
                query = query.Where(t => t.CutoffDateId == cutoffDateId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string cleanSearch = search.Trim().ToLower();
                query = query.Where(t =>
                    t.Subject!.Name.ToLower().Contains(cleanSearch) ||
                    t.Subject.Code.ToLower().Contains(cleanSearch) ||
                    t.Subject.DocenteAsignaturas.Any(da => da.Docente != null && da.Docente.FullName.ToLower().Contains(cleanSearch)));
            }

            if (onlyHighRisk)
            {
                query = query.Where(t => t.FailurePercentage >= 40.0m);
            }

            var trackings = await query
                .OrderBy(t => t.Subject!.Name)
                .ThenBy(t => t.CutoffDateId)
                .ThenBy(t => t.UnitNumber)
                .ToListAsync();

            var rows = trackings.Select(t => new TrackingReportRowItem
            {
                TrackingId = t.Id,
                SubjectId = t.SubjectId,
                SubjectCode = t.Subject?.Code ?? "-",
                SubjectName = t.Subject?.Name ?? "-",
                TeacherName = t.Subject?.DocenteAsignaturas?.FirstOrDefault()?.Docente?.FullName ?? "Sin asignar",
                CareerName = t.Subject?.Departamento?.Name ?? "General",
                PhaseName = t.CutoffDate?.Name ?? "Seguimiento",
                UnitNumber = t.UnitNumber,
                TotalStudents = t.TotalStudents,
                ApprovedStudents = t.ApprovedStudents,
                FailedStudents = t.FailedStudents,
                DroppedStudents = t.DroppedStudents,
                ApprovalPercentage = t.ApprovalPercentage,
                FailurePercentage = t.FailurePercentage,
                DropoutPercentage = t.DropoutPercentage,
                CorrectiveAction = t.Observations,
                ApprovalStatus = t.ApprovalStatus
            }).ToList();

            var cutoffDatesQuery = _context.CutoffDates
                .Where(c => c.AcademicPeriodId == activePeriodId && c.PhaseType != "Inicial");

            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                cutoffDatesQuery = cutoffDatesQuery.Where(c => c.DepartamentoId == currentUser.DepartamentoId || c.DepartamentoId == null);
            }

            var viewModel = new GeneralTrackingReportViewModel
            {
                SelectedPeriodId = activePeriodId,
                SelectedCutoffDateId = cutoffDateId,
                SearchTeacherOrSubject = search,
                OnlyHighRisk = onlyHighRisk,
                AcademicPeriods = await _context.AcademicPeriods.OrderByDescending(p => p.Id).ToListAsync(),
                CutoffDates = await cutoffDatesQuery.ToListAsync(),
                TrackingRows = rows,
                TotalGroupsEvaluated = rows.Select(r => r.SubjectId).Distinct().Count(),
                TotalHighRiskUnits = rows.Count(r => r.IsHighRisk),
                GlobalApprovalAverage = rows.Any() ? Math.Round(rows.Average(r => r.ApprovalPercentage), 1) : 0,
                GlobalFailureAverage = rows.Any() ? Math.Round(rows.Average(r => r.FailurePercentage), 1) : 0
            };

            return View(viewModel);
        }
    }
}