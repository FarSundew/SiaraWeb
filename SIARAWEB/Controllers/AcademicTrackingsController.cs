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

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var periodoActivo = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive);
            if (periodoActivo == null) return View(new List<Subject>());

            var misAsignaturas = await _context.DocenteAsignaturas
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.AcademicTrackings)
                .Where(da => da.DocenteId == currentUser.Id && da.AcademicPeriodId == periodoActivo.Id)
                .Select(da => da.Subject!)
                .ToListAsync();

            return View(misAsignaturas);
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

            bool esSoloLectura = task.DueDate < DateTime.Now || !task.IsActive;

            ViewBag.Subject = subject;
            ViewBag.Task = task;
            ViewBag.EsSoloLectura = esSoloLectura;

            int nextUnit = 1; if (subject.AcademicTrackings != null && subject.AcademicTrackings.Any()) { nextUnit = subject.AcademicTrackings.Max(t => t.UnitNumber) + 1; } return View(new AcademicTracking { SubjectId = id, DocumentTaskId = task.Id, UnitNumber = nextUnit });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Capture(AcademicTracking tracking)
        {
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

            var task = await _context.DocumentTasks.FirstOrDefaultAsync(t => t.Id == tracking.DocumentTaskId);

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

