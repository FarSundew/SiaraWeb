using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;
using SIARAWEB.ViewModels;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "Docente,JefeCarrera,JefeGeneral,Administrador")]
    public class FinalReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FinalReportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: FinalReport/SubjectSummary/5
        public async Task<IActionResult> SubjectSummary(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.AcademicPeriod)
                .Include(s => s.Departamento)
                .Include(s => s.DocenteAsignaturas!)
                    .ThenInclude(da => da.Docente)
                .Include(s => s.AcademicTrackings!)
                    .ThenInclude(at => at.CutoffDate)
                .Include(s => s.Documents!)
                    .ThenInclude(d => d.CutoffDate)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            // 1. Buscar la fecha de corte para Reporte Final (priorizando departamento)
            var cutoffFinal = await _context.CutoffDates
                .Where(c => c.AcademicPeriodId == subject.AcademicPeriodId &&
                           (c.DepartamentoId == subject.DepartamentoId || c.DepartamentoId == null) &&
                            c.PhaseType == "Final")
                .OrderByDescending(c => c.DepartamentoId)
                .FirstOrDefaultAsync();

            // 2. Solo lectura si ya expiró el corte o el usuario es solo visor (JefeGeneral)
            bool esSoloLectura = User.IsInRole("JefeGeneral") || (cutoffFinal != null && DateTime.Now > cutoffFinal.DueDate);

            // 3. Obtener unidades previas
            var units = subject.AcademicTrackings?.OrderBy(u => u.UnitNumber).ToList() ?? new List<AcademicTracking>();

            // 4. Buscar registro de cierre existente
            var existingFinal = await _context.FinalSubjectGrades
                .FirstOrDefaultAsync(f => f.SubjectId == id);

            // 5. Documentos entregados en fase final
            var finalDocs = subject.Documents?
                .Where(d => d.CutoffDate != null && d.CutoffDate.PhaseType == "Final")
                .ToList() ?? new List<Document>();

            var viewModel = new FinalReportConsolidatedViewModel
            {
                Subject = subject,
                CutoffDateFinal = cutoffFinal,
                IsReadOnly = esSoloLectura,
                UnitTrackings = units,
                FinalDocuments = finalDocs,
                FinalGrade = existingFinal ?? new FinalSubjectGrade
                {
                    SubjectId = id,
                    CutoffDateId = cutoffFinal?.Id ?? 0,
                    TotalStudents = units.FirstOrDefault()?.TotalStudents ?? 0
                },
                UnitsAverageApproval = units.Any() ? Math.Round(units.Average(u => u.ApprovalPercentage), 2) : 0,
                UnitsAverageFailure = units.Any() ? Math.Round(units.Average(u => u.FailurePercentage), 2) : 0,
                UnitsAverageDropout = units.Any() ? Math.Round(units.Average(u => u.DropoutPercentage), 2) : 0
            };

            return View(viewModel);
        }

        // POST: FinalReport/SaveFinalGrades
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Docente,JefeCarrera")]
        public async Task<IActionResult> SaveFinalGrades(FinalSubjectGrade model)
        {
            if (model.TotalStudents <= 0)
            {
                TempData["Error"] = "El total de alumnos inscritos debe ser mayor a 0.";
                return RedirectToAction(nameof(SubjectSummary), new { id = model.SubjectId });
            }

            // Cálculo institucional definitivo[cite: 2]
            model.FinalApprovedStudents = model.ApprovedRegularStudents + model.ApprovedMakeupStudents;
            model.FinalApprovalPercentage = Math.Round(((decimal)model.FinalApprovedStudents / model.TotalStudents) * 100, 2);
            model.FinalFailurePercentage = Math.Round(((decimal)model.FinalFailedStudents / model.TotalStudents) * 100, 2);
            model.FinalDropoutPercentage = Math.Round(((decimal)model.FinalDroppedStudents / model.TotalStudents) * 100, 2);

            var existing = await _context.FinalSubjectGrades.FirstOrDefaultAsync(f => f.SubjectId == model.SubjectId);

            if (existing == null)
            {
                model.RegisteredAt = DateTime.Now;
                model.ApprovalStatus = "Pendiente";
                _context.FinalSubjectGrades.Add(model);
            }
            else
            {
                existing.ApprovedRegularStudents = model.ApprovedRegularStudents;
                existing.ApprovedMakeupStudents = model.ApprovedMakeupStudents;
                existing.FinalApprovedStudents = model.FinalApprovedStudents;
                existing.FinalFailedStudents = model.FinalFailedStudents;
                existing.FinalDroppedStudents = model.FinalDroppedStudents;
                existing.FinalApprovalPercentage = model.FinalApprovalPercentage;
                existing.FinalFailurePercentage = model.FinalFailurePercentage;
                existing.FinalDropoutPercentage = model.FinalDropoutPercentage;
                existing.Observations = model.Observations;
                existing.RegisteredAt = DateTime.Now;
                existing.ApprovalStatus = "Pendiente";
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cierre de materia e índices finales guardados con éxito.";
            return RedirectToAction(nameof(SubjectSummary), new { id = model.SubjectId });
        }
    }
}