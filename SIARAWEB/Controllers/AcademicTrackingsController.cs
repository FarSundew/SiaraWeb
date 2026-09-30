using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "Docente,JefeCarrera")]
    public class AcademicTrackingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AcademicTrackingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: AcademicTrackings (Lista de materias del docente)
        // GET: AcademicTrackings
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var periodoActivo = await _context.AcademicPeriods
                .FirstOrDefaultAsync(p => p.IsActive);

            if (periodoActivo == null)
            {
                return View(new List<Subject>());
            }

            var misAsignaturas = await _context.DocenteAsignaturas
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.AcademicPeriod)
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.AcademicTrackings)
                .Where(da => da.DocenteId == currentUser.Id &&
                             da.Subject!.AcademicPeriodId == periodoActivo.Id) // 🟢 FILTRO PERIODO ACTIVO
                .Select(da => da.Subject!)
                .ToListAsync();

            return View(misAsignaturas);
        }

        // GET: AcademicTrackings/Capture/5
        public async Task<IActionResult> Capture(int id, string? faseSeleccionada)
        {
            var subject = await _context.Subjects
                .Include(s => s.AcademicPeriod)
                .Include(s => s.Departamento)
                .Include(s => s.AcademicTrackings!)
                    .ThenInclude(at => at.CutoffDate)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            // 🟢 FASE 4: Buscar la fecha de corte activa priorizando la fecha de su propio Departamento
            var faseActiva = await _context.CutoffDates
                .Where(c => c.AcademicPeriodId == subject.AcademicPeriodId &&
                           (c.DepartamentoId == subject.DepartamentoId || c.DepartamentoId == null) &&
                            c.DueDate >= DateTime.Now)
                .OrderByDescending(c => c.DepartamentoId) // Prioriza la del departamento sobre la general
                .ThenBy(c => c.DueDate)
                .FirstOrDefaultAsync();

            // 2. Determinamos la fase visualizada (por defecto Seguimiento1 si no hay activa o seleccionada)
            string faseActual = faseSeleccionada ?? faseActiva?.PhaseType ?? "Seguimiento1";

            // 3. Modo de solo lectura si la fecha expiró o se visualiza un corte distinto
            bool esSoloLectura = faseActiva == null || faseActiva.PhaseType != faseActual;

            // 🟢 FASE 4: Fechas de corte disponibles priorizando el departamento (excluyendo Inicial)
            var cutoffDatesQuery = _context.CutoffDates
                .Where(c => c.AcademicPeriodId == subject.AcademicPeriodId &&
                           (c.DepartamentoId == subject.DepartamentoId || c.DepartamentoId == null) &&
                            c.PhaseType != "Inicial")
                .OrderBy(c => c.StartDate);

            ViewBag.CutoffDates = new SelectList(cutoffDatesQuery, "Id", "Name");
            ViewBag.Subject = subject;
            ViewBag.FaseActiva = faseActiva?.PhaseType;
            ViewBag.FaseActual = faseActual;
            ViewBag.EsSoloLectura = esSoloLectura;

            return View(new AcademicTracking { SubjectId = id });
        }

        // POST: AcademicTrackings/Capture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Capture(AcademicTracking tracking)
        {
            // Evita conflictos de inserción con la columna IDENTITY en SQL Server
            tracking.Id = 0;

            // Cálculo de métricas porcentuales si hay alumnos registrados
            if (tracking.TotalStudents > 0)
            {
                tracking.ApprovalPercentage = Math.Round(((decimal)tracking.ApprovedStudents / tracking.TotalStudents) * 100, 2);
                tracking.FailurePercentage = Math.Round(((decimal)tracking.FailedStudents / tracking.TotalStudents) * 100, 2);
                tracking.DropoutPercentage = Math.Round(((decimal)tracking.DroppedStudents / tracking.TotalStudents) * 100, 2);
            }

            // 🔒 Regla institucional: Si la reprobación es >= 40%, la acción correctiva (Observations) es obligatoria
            if (tracking.FailurePercentage >= 40 && string.IsNullOrWhiteSpace(tracking.Observations))
            {
                ModelState.AddModelError(nameof(tracking.Observations), "La acción correctiva es obligatoria cuando el índice de reprobación es igual o superior al 40%.");
            }

            if (ModelState.IsValid)
            {
                tracking.CreatedAt = DateTime.Now;
                tracking.ApprovalStatus = "Pendiente";

                _context.AcademicTrackings.Add(tracking);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Métricas del Tema {tracking.UnitNumber} registradas exitosamente.";
                return RedirectToAction(nameof(Capture), new { id = tracking.SubjectId });
            }

            // Recarga de dependencias en caso de validación fallida
            var subject = await _context.Subjects
                .Include(s => s.AcademicPeriod)
                .Include(s => s.Departamento)
                .Include(s => s.AcademicTrackings!)
                    .ThenInclude(at => at.CutoffDate)
                .FirstOrDefaultAsync(s => s.Id == tracking.SubjectId);

            if (subject == null) return NotFound();

            // 🟢 FASE 4: Recalcular la fecha activa con filtro de Departamento
            var faseActivaRecarga = await _context.CutoffDates
                .Where(c => c.AcademicPeriodId == subject.AcademicPeriodId &&
                           (c.DepartamentoId == subject.DepartamentoId || c.DepartamentoId == null) &&
                            c.DueDate >= DateTime.Now)
                .OrderByDescending(c => c.DepartamentoId)
                .ThenBy(c => c.DueDate)
                .FirstOrDefaultAsync();

            var cutoffDatesQueryRecarga = _context.CutoffDates
                .Where(c => c.AcademicPeriodId == subject.AcademicPeriodId &&
                           (c.DepartamentoId == subject.DepartamentoId || c.DepartamentoId == null) &&
                            c.PhaseType != "Inicial")
                .OrderBy(c => c.StartDate);

            ViewBag.CutoffDates = new SelectList(cutoffDatesQueryRecarga, "Id", "Name", tracking.CutoffDateId);
            ViewBag.Subject = subject;
            ViewBag.FaseActiva = faseActivaRecarga?.PhaseType;
            ViewBag.FaseActual = faseActivaRecarga?.PhaseType ?? "Seguimiento1";
            ViewBag.EsSoloLectura = false;

            return View(tracking);
        }

        // POST: AcademicTrackings/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int subjectId)
        {
            var tracking = await _context.AcademicTrackings.FindAsync(id);
            if (tracking != null)
            {
                _context.AcademicTrackings.Remove(tracking);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Registro de tema eliminado.";
            }

            return RedirectToAction(nameof(Capture), new { id = subjectId });
        }
    }
}