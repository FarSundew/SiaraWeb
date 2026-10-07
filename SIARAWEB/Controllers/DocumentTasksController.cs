using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SIARAWEB.Controllers
{
    // ?? 1. A NIVEL CONTROLADOR: Solo exige estar autenticado (no bloquea al docente)
    [Authorize]
    public class DocumentTasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DocumentTasksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================================
        // BANDEJA DEL DOCENTE (Estilo Classroom / Teams)
        // =========================================================================
        // ?? 2. ACCESO PARA DOCENTES (y Jefes que dan clase)
        [Authorize(Roles = "Docente,JefeCarrera,Administrador")]
        public async Task<IActionResult> DocenteTasks()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            // 1. Tareas activas
            var tareas = await _context.DocumentTasks
                .Include(t => t.AcademicPeriod)
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.DueDate)
                .ToListAsync();

            // 2. Asignaturas de este docente con sus documentos y seguimientos
            var misMaterias = await _context.DocenteAsignaturas
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.Documents)
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.AcademicTrackings)
                .Where(da => da.DocenteId == user.Id)
                .ToListAsync();

            ViewBag.MisMaterias = misMaterias;
            return View(tareas);
        }

        // =========================================================================
        // ACCIONES DE GESTI?N Y REVISI?N (Jefes y Administrador)
        // =========================================================================

        // GET: DocumentTasks
        [Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var query = _context.DocumentTasks
                .Include(t => t.AcademicPeriod)
                .Include(t => t.Departamento)
                .AsQueryable();

            if (User.IsInRole("JefeCarrera") && user?.DepartamentoId != null)
            {
                query = query.Where(t => t.DepartamentoId == user.DepartamentoId);
            }

            var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return View(tasks);
        }

        // GET: DocumentTasks/Details/5
        [Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]
        public async Task<IActionResult> Details(int id)
        {
            var task = await _context.DocumentTasks
                .Include(t => t.AcademicPeriod)
                .Include(t => t.Departamento)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            return View(task);
        }

        // GET: DocumentTasks/Create
        [Authorize(Roles = "JefeCarrera,Administrador")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: DocumentTasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera,Administrador")]
        public async Task<IActionResult> Create(string title, string semestre, int anio, string phaseType, DateTime dueDate, string? description)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(semestre) || anio < 2020)
            {
                ModelState.AddModelError("", "Todos los campos obligatorios deben ser completados.");
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            var departamentoId = user?.DepartamentoId ?? 1;

            string periodName = $"{semestre.Trim()} {anio}".Trim();
            var period = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.Name == periodName);

            if (period == null)
            {
                period = new AcademicPeriod
                {
                    Name = periodName,
                    StartDate = semestre.Contains("Julio", StringComparison.OrdinalIgnoreCase)
                        ? new DateTime(anio, 7, 1)
                        : new DateTime(anio, 1, 1),
                    EndDate = semestre.Contains("Julio", StringComparison.OrdinalIgnoreCase)
                        ? new DateTime(anio, 12, 31)
                        : new DateTime(anio, 6, 30),
                    IsActive = true
                };
                _context.AcademicPeriods.Add(period);
                await _context.SaveChangesAsync();
            }

            var nuevaTarea = new DocumentTask
            {
                Title = title.Trim(),
                Description = description,
                PhaseType = phaseType,
                DueDate = dueDate,
                AcademicPeriodId = period.Id,
                DepartamentoId = departamentoId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.DocumentTasks.Add(nuevaTarea);
            await _context.SaveChangesAsync();

            // NOTIFICACIONES
            var docentesAfectados = await _context.DocenteAsignaturas
                .Include(da => da.Subject)
                .Where(da => da.AcademicPeriodId == period.Id && da.Subject != null && da.Subject.DepartamentoId == departamentoId)
                .Select(da => da.DocenteId)
                .Distinct()
                .ToListAsync();
            
            foreach (var docId in docentesAfectados)
            {
                var notif = new Notification
                {
                    UserId = docId,
                    Message = "Nueva Tarea: " + nuevaTarea.Title + " - Vence: " + nuevaTarea.DueDate.ToString("dd/MMM/yyyy"),
                    ActionUrl = "/DocumentTasks/DocenteTasks",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    Type = "Importante"
                };
                _context.Notifications.Add(notif);
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tarea/Buz�n de entrega publicado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: DocumentTasks/Edit/5
        [Authorize(Roles = "JefeCarrera,Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.DocumentTasks
                .Include(t => t.AcademicPeriod)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            return View(task);
        }

        // POST: DocumentTasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera,Administrador")]
        public async Task<IActionResult> Edit(int id, string title, string semestre, int anio, string phaseType, DateTime dueDate, string? description)
        {
            var task = await _context.DocumentTasks.FindAsync(id);
            if (task == null) return NotFound();

            string periodName = $"{semestre.Trim()} {anio}".Trim();
            var period = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.Name == periodName);

            if (period == null)
            {
                period = new AcademicPeriod
                {
                    Name = periodName,
                    StartDate = semestre.Contains("Julio", StringComparison.OrdinalIgnoreCase)
                        ? new DateTime(anio, 7, 1)
                        : new DateTime(anio, 1, 1),
                    EndDate = semestre.Contains("Julio", StringComparison.OrdinalIgnoreCase)
                        ? new DateTime(anio, 12, 31)
                        : new DateTime(anio, 6, 30),
                    IsActive = true
                };
                _context.AcademicPeriods.Add(period);
                await _context.SaveChangesAsync();
            }

            task.Title = title.Trim();
            task.Description = description;
            task.PhaseType = phaseType;
            task.DueDate = dueDate;
            task.AcademicPeriodId = period.Id;

            _context.DocumentTasks.Update(task);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tarea/Buz?n actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: DocumentTasks/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera,Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.DocumentTasks
                .Include(t => t.Documents)
                .Include(t => t.AcademicTrackings)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            if (task.Documents.Any() || task.AcademicTrackings.Any())
            {
                TempData["Error"] = "No se puede eliminar la tarea porque ya cuenta con entregas registradas.";
                return RedirectToAction(nameof(Index));
            }

            _context.DocumentTasks.Remove(task);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tarea eliminada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: DocumentTasks/Review/5
        [Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]
        public async Task<IActionResult> Review(int id)
        {
            var task = await _context.DocumentTasks
                .Include(t => t.AcademicPeriod)
                .Include(t => t.Departamento)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

                        var asignaciones = await _context.DocenteAsignaturas
                .Include(da => da.Docente)
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.Documents!.Where(d => d.DocumentTaskId == id))
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.AcademicTrackings!.Where(at => at.DocumentTaskId == id))
                .Where(da => da.AcademicPeriodId == task.AcademicPeriodId && da.Subject!.DepartamentoId == task.DepartamentoId)
                .OrderBy(da => da.Subject!.Name)
                .ThenBy(da => da.Group)
                .ToListAsync();

            ViewBag.Task = task;
            return View(asignaciones);
        }

        // POST: DocumentTasks/ReviewDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera,Administrador")]
                public async Task<IActionResult> ReviewDocument(int documentId, int taskId, string status, string? feedback)
        {
            var document = await _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.DocumentTask)
                .FirstOrDefaultAsync(d => d.Id == documentId);
                
            if (document != null)
            {
                document.ApprovalStatus = status;
                document.Feedback = feedback ?? string.Empty;

                _context.Documents.Update(document);
                
                // NOTIFICACION AL DOCENTE
                var docAsig = await _context.DocenteAsignaturas
                    .FirstOrDefaultAsync(da => da.SubjectId == document.SubjectId && da.AcademicPeriodId == document.DocumentTask.AcademicPeriodId);
                    
                if (docAsig != null)
                {
                    var notif = new Notification
                    {
                        UserId = docAsig.DocenteId,
                        Message = $"Tu documento de {document.DocumentType} en {document.Subject?.Name} fue {status.ToLower()}",
                        ActionUrl = "/DocumentTasks/DocenteTasks",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        Type = "Normal"
                    };
                    _context.Notifications.Add(notif);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Documento {status.ToLower()} exitosamente.";
            }

            return RedirectToAction(nameof(Review), new { id = taskId });
        }
        
        [HttpPost]
        [Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]
        [ValidateAntiForgeryToken]
                public async Task<IActionResult> ReviewTracking(int trackingId, int taskId, string status, string? feedback)
        {
            var tracking = await _context.AcademicTrackings
                .Include(t => t.Subject)
                .Include(t => t.DocumentTask)
                .FirstOrDefaultAsync(t => t.Id == trackingId);
                
            if (tracking == null) return NotFound();

            tracking.ApprovalStatus = status;
            tracking.Feedback = feedback;
            _context.Update(tracking);
            
            // NOTIFICACION AL DOCENTE
            var docAsig = await _context.DocenteAsignaturas
                .FirstOrDefaultAsync(da => da.SubjectId == tracking.SubjectId && da.AcademicPeriodId == tracking.DocumentTask.AcademicPeriodId);
                
            if (docAsig != null)
            {
                var notif = new Notification
                {
                    UserId = docAsig.DocenteId,
                    Message = $"Tu seguimiento de la Unidad {tracking.UnitNumber} en {tracking.Subject?.Name} fue {status.ToLower()}",
                    ActionUrl = "/DocumentTasks/DocenteTasks",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    Type = "Normal"
                };
                _context.Notifications.Add(notif);
            }

            await _context.SaveChangesAsync();

            if (status == "Aprobado")
            {
                TempData["Success"] = "Seguimiento aprobado exitosamente.";
            }
            else
            {
                TempData["Success"] = "Seguimiento rechazado exitosamente.";
            }

            return RedirectToAction(nameof(Review), new { id = taskId });
        }

                // GET: DocumentTasks/SubjectHistory/5
        [Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]
        public async Task<IActionResult> SubjectHistory(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Departamento)
                .Include(s => s.DocenteAsignaturas!)
                    .ThenInclude(da => da.Docente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            var documents = await _context.Documents
                .Include(d => d.DocumentTask)
                .Where(d => d.SubjectId == id && d.DocumentTaskId != null)
                .ToListAsync();
                
            var trackings = await _context.AcademicTrackings
                .Include(t => t.DocumentTask)
                .Where(t => t.SubjectId == id && t.DocumentTaskId != null)
                .ToListAsync();
                
            var taskIds = documents.Select(d => d.DocumentTaskId.Value)
                .Union(trackings.Select(t => t.DocumentTaskId.Value))
                .Distinct()
                .ToList();
                
            var tasks = await _context.DocumentTasks
                .Where(t => taskIds.Contains(t.Id))
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            var viewModel = new SIARAWEB.ViewModel.SubjectHistoryViewModel
            {
                Subject = subject,
                Docente = subject.DocenteAsignaturas?.FirstOrDefault()?.Docente,
                TaskGroups = tasks.Select(t => new SIARAWEB.ViewModel.TaskHistoryGroup
                {
                    Task = t,
                    Documents = documents.Where(d => d.DocumentTaskId == t.Id).ToList(),
                    Trackings = trackings.Where(tr => tr.DocumentTaskId == t.Id).OrderBy(tr => tr.UnitNumber).ToList()
                }).OrderByDescending(g => g.Task.DueDate).ToList()
            };

            return View(viewModel);
        }
    }
}







