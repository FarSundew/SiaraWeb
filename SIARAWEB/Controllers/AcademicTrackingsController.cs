using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "Docente,JefeCarrera,Administrador")]
    public class AcademicTrackingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AcademicTrackingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Capture(int id, int taskId)
        {
            var subject = await _context.Subjects
                .Include(s => s.Departamento)
                .Include(s => s.AcademicTrackings!)
                    .ThenInclude(at => at.DocumentTask)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            var task = await _context.DocumentTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return NotFound("Tarea no encontrada.");

            // 🔒 REGLA DE NEGOCIO: Entrega secuencial de seguimientos
            if (task.PhaseType == "Seguimiento2")
            {
                var tieneSeg1 = await _context.AcademicTrackings.AnyAsync(at =>
                    at.SubjectId == id &&
                    at.DocumentTask != null &&
                    at.DocumentTask.AcademicPeriodId == task.AcademicPeriodId &&
                    at.DocumentTask.PhaseType == "Seguimiento1");

                if (!tieneSeg1)
                {
                    TempData["Error"] = "Entrega secuencial bloqueada: Debes capturar y entregar las calificaciones del 1er Seguimiento antes de registrar el 2do Seguimiento.";
                    return RedirectToAction("DocenteTasks", "DocumentTasks");
                }
            }
            else if (task.PhaseType == "Final")
            {
                var tieneSeg2 = await _context.AcademicTrackings.AnyAsync(at =>
                    at.SubjectId == id &&
                    at.DocumentTask != null &&
                    at.DocumentTask.AcademicPeriodId == task.AcademicPeriodId &&
                    at.DocumentTask.PhaseType == "Seguimiento2");

                if (!tieneSeg2)
                {
                    TempData["Error"] = "Entrega secuencial bloqueada: Debes capturar y entregar las calificaciones del 2do Seguimiento antes de registrar el Reporte Final.";
                    return RedirectToAction("DocenteTasks", "DocumentTasks");
                }
            }

            bool esSoloLectura = task.DueDate < DateTime.Now || !task.IsActive;

            ViewBag.Subject = subject;
            ViewBag.Task = task;
            ViewBag.EsSoloLectura = esSoloLectura;

            int nextUnit = 1;
            if (subject.AcademicTrackings != null && subject.AcademicTrackings.Any(at => at.DocumentTaskId == taskId))
            {
                nextUnit = subject.AcademicTrackings.Where(at => at.DocumentTaskId == taskId).Max(t => t.UnitNumber) + 1;
            }
            return View(new AcademicTracking { SubjectId = id, DocumentTaskId = task.Id, UnitNumber = nextUnit });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Capture(AcademicTracking tracking)
        {
            var task = await _context.DocumentTasks.FirstOrDefaultAsync(t => t.Id == tracking.DocumentTaskId);
            if (task == null) return NotFound();

            // 🔒 REGLA DE NEGOCIO: Entrega secuencial de seguimientos
            if (task.PhaseType == "Seguimiento2")
            {
                var tieneSeg1 = await _context.AcademicTrackings.AnyAsync(at =>
                    at.SubjectId == tracking.SubjectId &&
                    at.DocumentTask != null &&
                    at.DocumentTask.AcademicPeriodId == task.AcademicPeriodId &&
                    at.DocumentTask.PhaseType == "Seguimiento1");

                if (!tieneSeg1)
                {
                    TempData["Error"] = "Entrega secuencial bloqueada: Debes capturar el 1er Seguimiento antes del 2do Seguimiento.";
                    return RedirectToAction("DocenteTasks", "DocumentTasks");
                }
            }
            else if (task.PhaseType == "Final")
            {
                var tieneSeg2 = await _context.AcademicTrackings.AnyAsync(at =>
                    at.SubjectId == tracking.SubjectId &&
                    at.DocumentTask != null &&
                    at.DocumentTask.AcademicPeriodId == task.AcademicPeriodId &&
                    at.DocumentTask.PhaseType == "Seguimiento2");

                if (!tieneSeg2)
                {
                    TempData["Error"] = "Entrega secuencial bloqueada: Debes capturar el 2do Seguimiento antes del Reporte Final.";
                    return RedirectToAction("DocenteTasks", "DocumentTasks");
                }
            }

            tracking.Id = 0;

            if (tracking.TotalStudents > 0)
            {
                tracking.ApprovalPercentage = Math.Round(((decimal)tracking.ApprovedStudents / tracking.TotalStudents) * 100, 2);
                tracking.FailurePercentage = Math.Round(((decimal)tracking.FailedStudents / tracking.TotalStudents) * 100, 2);
                tracking.DropoutPercentage = Math.Round(((decimal)tracking.DroppedStudents / tracking.TotalStudents) * 100, 2);
            }

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
                return RedirectToAction(nameof(Capture), new { id = tracking.SubjectId, taskId = tracking.DocumentTaskId });
            }

            var subject = await _context.Subjects
                .Include(s => s.Departamento)
                .Include(s => s.AcademicTrackings!)
                    .ThenInclude(at => at.DocumentTask)
                .FirstOrDefaultAsync(s => s.Id == tracking.SubjectId);

            ViewBag.Subject = subject;
            ViewBag.Task = task;
            ViewBag.EsSoloLectura = task == null || task.DueDate < DateTime.Now || !task.IsActive;

            return View(tracking);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int subjectId, int taskId)
        {
            var tracking = await _context.AcademicTrackings.FindAsync(id);
            if (tracking != null)
            {
                _context.AcademicTrackings.Remove(tracking);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Registro de tema eliminado.";
            }
            return RedirectToAction(nameof(Capture), new { id = subjectId, taskId = taskId });
        }
    }
}

