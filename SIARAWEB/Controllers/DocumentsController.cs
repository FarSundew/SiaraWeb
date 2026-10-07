using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    // ?? Autorizamos tanto a Docentes puros como a Jefes de Carrera que imparten clases
    [Authorize(Roles = "Docente,JefeCarrera")]
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DocumentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Documents (Mis Materias)
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var misAsignaturas = await _context.DocenteAsignaturas
                .Include(da => da.Subject)

                .Include(da => da.Subject)
                    .ThenInclude(s => s!.Documents)
                .Where(da => da.DocenteId == currentUser.Id)
                .Select(da => da.Subject)
                .ToListAsync();

            return View(misAsignaturas);
        }

        // GET: Documents/Upload/5
        public async Task<IActionResult> Upload(int id, int taskId)
        {
            var subject = await _context.Subjects
                .Include(s => s.Documents)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            var task = await _context.DocumentTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return NotFound("La tarea/buzón no existe.");

            var documentosRequeridos = new List<string>();
            switch (task.PhaseType)
            {
                case "Inicial":
                    documentosRequeridos = new List<string> {
                        "Instrumentación Didáctica",
                        "Instrumentos de Evaluación",
                        "Prácticas de Laboratorio",
                        "Proyecto Individual",
                        "Evaluación Diagnóstica"
                    };
                    break;
                case "Seguimiento1":
                    documentosRequeridos = new List<string> {
                        "Avance (apart. 6)",
                        "Calif. Parc. (Calificaciones Parciales)",
                        "Instr. Eval. (Instrumentos de Evaluación)",
                        "Eval. Diagn. (Evaluación Diagnóstica)",
                        "Avance Proy. Ind. (Proyecto Individual)"
                    };
                    break;
                case "Seguimiento2":
                    documentosRequeridos = new List<string> {
                        "Avance Programático (apart. 6)",
                        "Instrumentos de Evaluación",
                        "Reporte de Seguimiento Intermedio"
                    };
                    break;
                case "Final":
                    documentosRequeridos = new List<string> {
                        "Acta de Calificaciones",
                        "Instrumentos de Evaluación Finales",
                        "Cierre de Proyecto / Reporte Final"
                    };
                    break;
            }

            ViewBag.Task = task;
            ViewBag.DocumentosRequeridos = documentosRequeridos;

            return View(subject);
        }

        // POST: Documents/UploadTaskDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTaskDocument(int subjectId, int documentTaskId, string documentType, IFormFile? file, bool isNotApplicable = false)
        {
            if (documentTaskId == 0)
            {
                TempData["Error"] = "Error crítico: No hay una Fase activa para vincular este documento.";
                return RedirectToAction(nameof(Upload), new { id = subjectId, taskId = documentTaskId });
            }

            var task = await _context.DocumentTasks.FindAsync(documentTaskId);
            if (task == null) return NotFound();

            // Buscar si ya existe un registro previo de este documento para el mismo corte
            var existingDoc = await _context.Documents.FirstOrDefaultAsync(d =>
                d.SubjectId == subjectId &&
                d.DocumentTaskId == documentTaskId &&
                d.DocumentType.ToLower().Trim() == documentType.ToLower().Trim());

            // ?? Caso 1: Marcado como No Aplica (N/A)
            if (isNotApplicable)
            {
                if (existingDoc == null)
                {
                    var naDoc = new Document
                    {
                        SubjectId = subjectId,
                        DocumentTaskId = documentTaskId,
                        DocumentType = documentType,
                        FilePath = "N/A",
                        UploadedAt = DateTime.Now,
                        IsOnTime = true,
                        Status = "NoAplica",
                        ApprovalStatus = "Aprobado"
                    };
                    _context.Documents.Add(naDoc);
                }
                else
                {
                    existingDoc.FilePath = "N/A";
                    existingDoc.Status = "NoAplica";
                    existingDoc.CorrectionSubmissionDate = DateTime.Now;
                    existingDoc.ApprovalStatus = "Aprobado";
                    existingDoc.Feedback = null;
                    _context.Documents.Update(existingDoc);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"El documento '{documentType}' fue marcado como No Aplica.";
                return RedirectToAction(nameof(Upload), new { id = subjectId, taskId = documentTaskId });
            }

            // ?? Caso 2: Carga de archivo PDF
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Por favor, selecciona un archivo PDF válido.";
                return RedirectToAction(nameof(Upload), new { id = subjectId, taskId = documentTaskId });
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".pdf")
            {
                TempData["Error"] = "Solo se admiten documentos en formato PDF (.pdf).";
                return RedirectToAction(nameof(Upload), new { id = subjectId, taskId = documentTaskId });
            }

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documentos");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string dbRelativePath = "/uploads/documentos/" + uniqueFileName;
            string physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            DateTime fechaActual = DateTime.Now;

            if (existingDoc == null)
            {
                // ?? Primera entrega: Evalúa puntualidad contra la fecha límite del corte
                bool entregadoATiempo = fechaActual <= task.DueDate;

                var nuevoDocumento = new Document
                {
                    SubjectId = subjectId,
                    DocumentTaskId = documentTaskId,
                    DocumentType = documentType,
                    FilePath = dbRelativePath,
                    UploadedAt = fechaActual,
                    IsOnTime = entregadoATiempo,
                    Status = entregadoATiempo ? "EnTiempo" : "Atrasado",
                    ApprovalStatus = "Pendiente"
                };

                _context.Documents.Add(nuevoDocumento);
                TempData["Success"] = entregadoATiempo
                    ? $"Archivo '{documentType}' subido a tiempo con éxito."
                    : $"Archivo '{documentType}' subido con retraso.";
            }
            else
            {
                // ?? Re-subida / Corrección: Se elimina el archivo PDF físico anterior si existía
                if (existingDoc.FilePath != "N/A")
                {
                    var oldPhysicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingDoc.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        System.IO.File.Delete(oldPhysicalPath);
                    }
                }

                // Se actualiza la ruta y la fecha de corrección SIN penalizar UploadedAt ni IsOnTime
                existingDoc.FilePath = dbRelativePath;
                existingDoc.CorrectionSubmissionDate = fechaActual;
                existingDoc.ApprovalStatus = "Pendiente";
                existingDoc.Feedback = null;
                    _context.Documents.Update(existingDoc);

                TempData["Success"] = $"Corrección de '{documentType}' enviada para nueva revisión.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Upload), new { id = subjectId, taskId = documentTaskId });
        }

        // POST: Documents/DeleteDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, int subjectId)
        {
            var doc = await _context.Documents
                .Include(d => d.DocumentTask)
                .FirstOrDefaultAsync(d => d.Id == id);

            string? faseRetorno = doc?.DocumentTask?.PhaseType;

            if (doc != null)
            {
                // 1. Eliminar el archivo físico del servidor (si no es N/A)
                if (doc.FilePath != "N/A")
                {
                    var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", doc.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }

                // 2. Eliminar el registro de la base de datos
                _context.Documents.Remove(doc);
                await _context.SaveChangesAsync();

                TempData["Success"] = "ARCHIVO ELIMINADO CON ÉXITO";
            }

            return RedirectToAction(nameof(Upload), new { id = subjectId, taskId = doc?.DocumentTaskId ?? 0 });
        }

        // POST: Documents/SetNotApplicable
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetNotApplicable(int subjectId, int documentTaskId, string documentType)
        {
            var task = await _context.DocumentTasks.FindAsync(documentTaskId);

            var documentoExistente = await _context.Documents
                .FirstOrDefaultAsync(d => d.SubjectId == subjectId &&
                                          d.DocumentTaskId == documentTaskId &&
                                          d.DocumentType.ToLower().Trim() == documentType.ToLower().Trim());

            if (documentoExistente != null)
            {
                documentoExistente.Status = "NoAplica";
                documentoExistente.FilePath = "N/A";
                documentoExistente.CorrectionSubmissionDate = DateTime.Now;
                documentoExistente.ApprovalStatus = "Aprobado";
                documentoExistente.Feedback = null;
            }
            else
            {
                var nuevoDocumento = new Document
                {
                    SubjectId = subjectId,
                    DocumentTaskId = documentTaskId,
                    DocumentType = documentType,
                    FilePath = "N/A",
                    UploadedAt = DateTime.Now,
                    IsOnTime = true,
                    Status = "NoAplica",
                    ApprovalStatus = "Aprobado"
                };
                _context.Documents.Add(nuevoDocumento);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Se ha registrado que '{documentType}' no aplica para esta materia.";
            return RedirectToAction(nameof(Upload), new { id = subjectId, taskId = documentTaskId });
        }
    }
}
