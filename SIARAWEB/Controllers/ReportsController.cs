using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;
using SIARAWEB.Services;
using ClosedXML.Excel;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PdfGeneratorService _pdfService;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, PdfGeneratorService pdfService)
        {
            _context = context;
            _userManager = userManager;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> Index(int? periodoId, int? departamentoId, string? search, string? tab)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");
            ViewBag.ActiveTab = string.IsNullOrWhiteSpace(tab) ? "kpi" : tab.Trim().ToLower();

            // 1. Definir Periodo con Cookie
            int? storedPeriodo = null;
            if (periodoId.HasValue)
            {
                Response.Cookies.Append("SelectedPeriodoId", periodoId.Value.ToString(), new CookieOptions { Expires = DateTime.Now.AddDays(7) });
                storedPeriodo = periodoId.Value;
            }
            else
            {
                var cookie = Request.Cookies["SelectedPeriodoId"];
                if (int.TryParse(cookie, out int cId)) storedPeriodo = cId;
            }

            var periodoActivo = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive)
                                ?? await _context.AcademicPeriods.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
            int activePeriodId = storedPeriodo ?? periodoActivo?.Id ?? 0;

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
                    (s.DocenteAsignaturas != null && s.DocenteAsignaturas.Any(da => da.Docente != null && da.Docente.FullName.ToLower().Contains(cleanSearch))));
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
                    return docsFase != null && docsFase.Any(d => !d.IsOnTime) ? "bg-danger" : "bg-success";
                }

                item.ColorInicial = EvaluarSemaforo("Inicial");
                item.ColorSeg1 = EvaluarSemaforo("Seguimiento1");
                item.ColorSeg2 = EvaluarSemaforo("Seguimiento2");
                item.ColorFinal = EvaluarSemaforo("Final");
                listaSemaforo.Add(item);
            }

                        // 5. Armar la Sabana
            var subjectIds = asignaturas.Select(s => s.Id).ToList();
            var allTrackings = await _context.AcademicTrackings
                .Include(t => t.DocumentTask)
                .Where(t => subjectIds.Contains(t.SubjectId) && t.DocumentTask != null && t.DocumentTask.AcademicPeriodId == activePeriodId)
                .ToListAsync();

            var listaSabana = asignaturas.Select(s => {
                var subjectTrackings = allTrackings.Where(t => t.SubjectId == s.Id).ToList();
                return new SabanaRow
                {
                    SubjectId = s.Id,
                    SubjectCode = s.Code,
                    SubjectName = s.Name,
                    TeacherName = s.DocenteAsignaturas?.FirstOrDefault()?.Docente?.FullName ?? "Sin Asignar",
                    TotalTrackings = subjectTrackings.Count,
                    AvgApprovalPercentage = subjectTrackings.Any() ? Math.Round(subjectTrackings.Average(t => t.ApprovalPercentage), 1) : 0m,
                    AvgFailurePercentage = subjectTrackings.Any() ? Math.Round(subjectTrackings.Average(t => t.FailurePercentage), 1) : 0m,
                    AvgDropoutPercentage = subjectTrackings.Any() ? Math.Round(subjectTrackings.Average(t => t.DropoutPercentage), 1) : 0m,
                    HighRiskCount = subjectTrackings.Count(t => t.FailurePercentage >= 40.0m)
                };
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
                PromedioDesercion = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.DropoutPercentage), 1) : 0,
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
        public async Task<IActionResult> Details(int id, string? tab, int? periodoId, int? departamentoId, string? search)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var materia = await _context.Subjects
                .Include(s => s.Departamento)
                .Include(s => s.DocenteAsignaturas!).ThenInclude(da => da.Docente)
                .Include(s => s.Documents!).ThenInclude(d => d.DocumentTask)
                .Include(s => s.AcademicTrackings!).ThenInclude(t => t.DocumentTask)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (materia == null) return NotFound();

            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                if (materia.DepartamentoId != currentUser?.DepartamentoId) return Forbid();
            }

            ViewBag.ActiveTab = string.IsNullOrWhiteSpace(tab) ? "semaforo" : tab.Trim().ToLower();
            ViewBag.PeriodoId = periodoId;
            ViewBag.DepartamentoId = departamentoId;
            ViewBag.Search = search;

            return View(materia);
        }

        // GET: Reports/ExportExcel
        public async Task<IActionResult> ExportExcel(int? periodoId, int? departamentoId, string? search)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var periodoActivo = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive)
                                ?? await _context.AcademicPeriods.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
            int activePeriodId = periodoId ?? periodoActivo?.Id ?? 0;
            var periodo = await _context.AcademicPeriods.FindAsync(activePeriodId);
            string periodName = periodo?.Name ?? "Periodo";

            var subjectQuery = _context.Subjects
                .Include(s => s.Departamento)
                .Include(s => s.DocenteAsignaturas!.Where(da => da.AcademicPeriodId == activePeriodId)).ThenInclude(da => da.Docente)
                .Where(s => s.DocenteAsignaturas!.Any(da => da.AcademicPeriodId == activePeriodId))
                .AsQueryable();

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
                var cleanSearch = search.Trim().ToLower();
                subjectQuery = subjectQuery.Where(s =>
                    s.Name.ToLower().Contains(cleanSearch) ||
                    s.Code.ToLower().Contains(cleanSearch) ||
                    s.DocenteAsignaturas!.Any(da => da.Docente != null && da.Docente.FullName.ToLower().Contains(cleanSearch)));
            }

            var asignaturas = await subjectQuery.OrderBy(s => s.Code).ToListAsync();
            var subjectIds = asignaturas.Select(s => s.Id).ToList();

            var allTrackings = await _context.AcademicTrackings
                .Include(t => t.DocumentTask)
                .Where(t => subjectIds.Contains(t.SubjectId) && t.DocumentTask != null && t.DocumentTask.AcademicPeriodId == activePeriodId)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sabana_Rendimiento");

            // Encabezado institucional
            worksheet.Cell(1, 1).Value = "SISTEMA INTEGRAL DE ADMINISTRACIÓN Y REPORTES ACADÉMICOS (SIARA)";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(2, 1).Value = $"Sábana Concentrada de Rendimiento Académico - Periodo: {periodName}";
            worksheet.Cell(2, 1).Style.Font.Bold = true;
            worksheet.Cell(2, 1).Style.Font.FontSize = 12;

            // Encabezados de tabla
            int rowIdx = 4;
            var headers = new string[] {
                "Código", "Asignatura", "Docente Titular", "Departamento", "Seguimientos",
                "Aprobación (%)", "Reprobación (%)", "Deserción (%)", "Alertas (>=40%)", "Estado de Riesgo"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cell(rowIdx, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(17, 45, 96);
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            rowIdx++;
            foreach (var s in asignaturas)
            {
                var tracks = allTrackings.Where(t => t.SubjectId == s.Id).ToList();
                decimal avgAprob = tracks.Any() ? Math.Round(tracks.Average(t => t.ApprovalPercentage), 1) : 0m;
                decimal avgReprob = tracks.Any() ? Math.Round(tracks.Average(t => t.FailurePercentage), 1) : 0m;
                decimal avgDeserc = tracks.Any() ? Math.Round(tracks.Average(t => t.DropoutPercentage), 1) : 0m;
                int highRisk = tracks.Count(t => t.FailurePercentage >= 40.0m);
                bool isRisk = avgReprob >= 40.0m || highRisk > 0;

                worksheet.Cell(rowIdx, 1).Value = s.Code;
                worksheet.Cell(rowIdx, 2).Value = s.Name;
                worksheet.Cell(rowIdx, 3).Value = s.DocenteAsignaturas?.FirstOrDefault()?.Docente?.FullName ?? "Sin Asignar";
                worksheet.Cell(rowIdx, 4).Value = s.Departamento?.Name ?? "General";
                worksheet.Cell(rowIdx, 5).Value = tracks.Count;
                worksheet.Cell(rowIdx, 6).Value = avgAprob;
                worksheet.Cell(rowIdx, 7).Value = avgReprob;
                worksheet.Cell(rowIdx, 8).Value = avgDeserc;
                worksheet.Cell(rowIdx, 9).Value = highRisk;
                worksheet.Cell(rowIdx, 10).Value = isRisk ? "EN RIESGO" : "NORMAL";

                if (isRisk)
                {
                    worksheet.Row(rowIdx).Style.Fill.BackgroundColor = XLColor.FromArgb(254, 226, 226);
                    worksheet.Cell(rowIdx, 10).Style.Font.FontColor = XLColor.Red;
                    worksheet.Cell(rowIdx, 10).Style.Font.Bold = true;
                }

                rowIdx++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            string fileName = $"Sabana_Rendimiento_{periodName.Replace(" ", "_")}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Devuelve el PDF general, semáforo, sábana, por departamento o por docente
        public async Task<IActionResult> ExportPdf(int? periodoId, int? departamentoId, string? search, string? docenteId = null, string? tipo = "general")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var periodoActivo = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive)
                                ?? await _context.AcademicPeriods.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
            int activePeriodId = periodoId ?? periodoActivo?.Id ?? 0;
            var periodoSeleccionado = await _context.AcademicPeriods.FindAsync(activePeriodId) ?? periodoActivo;

            var subjectQuery = _context.Subjects
                .Include(s => s.Departamento)
                .Include(s => s.DocenteAsignaturas!.Where(da => da.AcademicPeriodId == activePeriodId)).ThenInclude(da => da.Docente).ThenInclude(d => d!.Departamento)
                .Include(s => s.Documents!.Where(d => d.DocumentTask != null && d.DocumentTask.AcademicPeriodId == activePeriodId)).ThenInclude(d => d.DocumentTask)
                .Include(s => s.AcademicTrackings!.Where(t => t.DocumentTask != null && t.DocumentTask.AcademicPeriodId == activePeriodId)).ThenInclude(t => t.DocumentTask)
                .Where(s => s.DocenteAsignaturas!.Any(da => da.AcademicPeriodId == activePeriodId))
                .AsQueryable();

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
                    (s.DocenteAsignaturas != null && s.DocenteAsignaturas.Any(da => da.Docente != null && da.Docente.FullName.ToLower().Contains(cleanSearch))));
            }

            var asignaturas = await subjectQuery.OrderBy(s => s.Code).ToListAsync();
            var subjectIds = asignaturas.Select(s => s.Id).ToList();

            var allTrackings = await _context.AcademicTrackings
                .Include(t => t.DocumentTask)
                .Where(t => subjectIds.Contains(t.SubjectId) && t.DocumentTask != null && t.DocumentTask.AcademicPeriodId == activePeriodId)
                .ToListAsync();

            byte[] pdfBytes;
            string fileName;
            string tipoNormalizado = (tipo ?? "general").Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(docenteId))
            {
                var docenteSubjects = asignaturas
                    .Where(s => s.DocenteAsignaturas != null && s.DocenteAsignaturas.Any(da => da.DocenteId == docenteId))
                    .ToList();

                var docente = await _context.Users
                    .Include(u => u.Departamento)
                    .FirstOrDefaultAsync(u => u.Id == docenteId);

                if (docente == null)
                {
                    return NotFound("El docente seleccionado no fue encontrado.");
                }

                var docenteSubjectIds = docenteSubjects.Select(s => s.Id).ToList();
                var trackings = allTrackings.Where(t => docenteSubjectIds.Contains(t.SubjectId)).ToList();

                pdfBytes = _pdfService.GeneratePdfByDocente(docente, docenteSubjects, trackings, periodoSeleccionado, currentUser.FullName);
                string cleanDocenteName = (docente.FullName ?? "Docente").Replace(" ", "_");
                fileName = $"Reporte_Docente_{cleanDocenteName}_{DateTime.Now:yyyyMMdd}.pdf";
            }
            else if (tipoNormalizado == "semaforo")
            {
                pdfBytes = _pdfService.GenerateSemaforoPdf(asignaturas, allTrackings, periodoSeleccionado, currentUser.FullName);
                fileName = $"Semaforo_Entregas_{DateTime.Now:yyyyMMdd}.pdf";
            }
            else if (tipoNormalizado == "sabana")
            {
                pdfBytes = _pdfService.GenerateSabanaPdf(asignaturas, allTrackings, periodoSeleccionado, currentUser.FullName);
                fileName = $"Sabana_Rendimiento_{DateTime.Now:yyyyMMdd}.pdf";
            }
            else
            {
                pdfBytes = _pdfService.GenerateReporteGeneralPdf(asignaturas, allTrackings, periodoSeleccionado, currentUser.FullName);
                fileName = $"Reporte_Ejecutivo_General_{DateTime.Now:yyyyMMdd}.pdf";
            }

            return File(pdfBytes, "application/pdf", fileName);
        }

        // Modal para seleccionar docente
        [HttpGet]
        public async Task<IActionResult> SelectDocente(int? periodoId, int? departamentoId, string? search)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            int? storedPeriodo = periodoId.HasValue ? periodoId : null;
            var periodoActivo = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive)
                                ?? await _context.AcademicPeriods.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
            int activePeriodId = storedPeriodo ?? periodoActivo?.Id ?? 0;

            var subjectQuery = _context.Subjects
                .Include(s => s.DocenteAsignaturas!.Where(da => da.AcademicPeriodId == activePeriodId))
                    .ThenInclude(da => da.Docente)
                        .ThenInclude(d => d!.Departamento)
                .Where(s => s.DocenteAsignaturas!.Any(da => da.AcademicPeriodId == activePeriodId))
                .AsQueryable();

            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                subjectQuery = subjectQuery.Where(s => s.DepartamentoId == currentUser.DepartamentoId);
            }
            else if (departamentoId.HasValue && departamentoId.Value > 0)
            {
                subjectQuery = subjectQuery.Where(s => s.DepartamentoId == departamentoId.Value);
            }

            var subjects = await subjectQuery.OrderBy(s => s.Code).ToListAsync();

            // Extraer docentes únicos
            var docentes = subjects
                .SelectMany(s => s.DocenteAsignaturas ?? new List<DocenteAsignatura>())
                .Select(da => da.Docente)
                .Where(d => d != null)
                .GroupBy(d => d!.Id)
                .Select(g => g.First()!)
                .OrderBy(d => d.FullName)
                .ToList();

            ViewBag.PeriodoId = activePeriodId;
            ViewBag.DepartamentoId = departamentoId;
            ViewBag.Search = search;
            return PartialView("_SelectDocente", docentes);
        }

        // Modal para seleccionar departamento
        [HttpGet]
        [Authorize(Roles = "Administrador,JefeGeneral")]
        public async Task<IActionResult> SelectDepartamento(int? periodoId)
        {
            var departamentos = await _context.Departamentos.OrderBy(d => d.Name).ToListAsync();
            ViewBag.PeriodoId = periodoId;
            return PartialView("_SelectDepartamento", departamentos);
        }
    }
}

































