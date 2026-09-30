using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    // 🟢 Autorizamos tanto a Docentes puros como a Jefes de Carrera que imparten clases
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
                    .ThenInclude(s => s!.AcademicPeriod)
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.Documents)
                .Where(da => da.DocenteId == currentUser.Id)
                .Select(da => da.Subject)
                .ToListAsync();

            return View(misAsignaturas);
        }

        // GET: Documents/Upload/5
        public async Task<IActionResult> Upload(int id, string? faseSeleccionada)
        {
            var subject = await _context.Subjects
                .Include(s => s.AcademicPeriod)
                .Include(s => s.Documents!)
                    .ThenInclude(d => d.CutoffDate)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            // 1. Buscamos la fecha activa dando prioridad a la de su departamento
            var faseActiva = await _context.CutoffDates
                .Where(c => c.AcademicPeriodId == subject.AcademicPeriodId &&
                           (c.DepartamentoId == subject.DepartamentoId || c.DepartamentoId == null) &&
                            c.DueDate >= DateTime.Now)
                .OrderByDescending(c => c.DepartamentoId) // Prioriza la del departamento sobre la general
                .ThenBy(c => c.DueDate)
                .FirstOrDefaultAsync();

            // 2. Definimos qué fase quiere ver el maestro
            string faseActual = faseSeleccionada ?? faseActiva?.PhaseType ?? "Inicial";

            // 3. Lógica de solo lectura
            bool esSoloLectura = faseActiva == null || faseActiva.PhaseType != faseActual;

            // 4. Cargamos los documentos correctos según la pestaña
            var documentosRequeridos = new List<string>();
            switch (faseActual)
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

            ViewBag.FaseActiva = faseActiva;
            ViewBag.FaseActual = faseActual;
            ViewBag.EsSoloLectura = esSoloLectura;
            ViewBag.DocumentosRequeridos = documentosRequeridos;

            return View(subject);
        }

        // POST: Documents/UploadFile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(int subjectId, int cutoffDateId, string documentType, IFormFile? file, bool isNotApplicable = false)
        {
            if (cutoffDateId == 0)
            {
                TempData["Error"] = "Error crítico: No hay una Fase activa para vincular este documento.";
                return RedirectToAction(nameof(Upload), new { id = subjectId });
            }

            var cutoffDate = await _context.CutoffDates.FindAsync(cutoffDateId);
            if (cutoffDate == null) return NotFound();

            // Buscar si ya existe un registro previo de este documento para el mismo corte
            var existingDoc = await _context.Documents.FirstOrDefaultAsync(d =>
                d.SubjectId == subjectId &&
                d.CutoffDateId == cutoffDateId &&
                d.DocumentType.ToLower().Trim() == documentType.ToLower().Trim());

            // 🟢 Caso 1: Marcado como No Aplica (N/A)
            if (isNotApplicable)
            {
                if (existingDoc == null)
                {
                    var naDoc = new Document
                    {
                        SubjectId = subjectId,
                        CutoffDateId = cutoffDateId,
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
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"El documento '{documentType}' fue marcado como No Aplica.";
                return RedirectToAction(nameof(Upload), new { id = subjectId, faseSeleccionada = cutoffDate.PhaseType });
            }

            // 🟢 Caso 2: Carga de archivo PDF
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Por favor, selecciona un archivo PDF válido.";
                return RedirectToAction(nameof(Upload), new { id = subjectId, faseSeleccionada = cutoffDate.PhaseType });
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".pdf")
            {
                TempData["Error"] = "Solo se admiten documentos en formato PDF (.pdf).";
                return RedirectToAction(nameof(Upload), new { id = subjectId, faseSeleccionada = cutoffDate.PhaseType });
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
                // 🔹 Primera entrega: Evalúa puntualidad contra la fecha límite del corte
                bool entregadoATiempo = fechaActual <= cutoffDate.DueDate;

                var nuevoDocumento = new Document
                {
                    SubjectId = subjectId,
                    CutoffDateId = cutoffDateId,
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
                // 🔹 Re-subida / Corrección: Se elimina el archivo PDF físico anterior si existía
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

                TempData["Success"] = $"Corrección de '{documentType}' enviada para nueva revisión.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Upload), new { id = subjectId, faseSeleccionada = cutoffDate.PhaseType });
        }

        // POST: Documents/DeleteDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, int subjectId)
        {
            var doc = await _context.Documents
                .Include(d => d.CutoffDate)
                .FirstOrDefaultAsync(d => d.Id == id);

            string? faseRetorno = doc?.CutoffDate?.PhaseType;

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

            return RedirectToAction(nameof(Upload), new { id = subjectId, faseSeleccionada = faseRetorno });
        }

        // POST: Documents/SetNotApplicable
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetNotApplicable(int subjectId, int cutoffDateId, string documentType)
        {
            var cutoff = await _context.CutoffDates.FindAsync(cutoffDateId);

            var documentoExistente = await _context.Documents
                .FirstOrDefaultAsync(d => d.SubjectId == subjectId &&
                                          d.CutoffDateId == cutoffDateId &&
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
                    CutoffDateId = cutoffDateId,
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
            return RedirectToAction(nameof(Upload), new { id = subjectId, faseSeleccionada = cutoff?.PhaseType });
        }
    }
}