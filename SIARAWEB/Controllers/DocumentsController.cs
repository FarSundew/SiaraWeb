using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    // 🟢 Autorizamos tanto a Docentes puros como a Jefes de Carrera que dan clases
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
            // 1. Identificar quién es el usuario que acaba de iniciar sesión
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account"); // O la ruta de tu login
            }

            // 2. Buscar SOLO las asignaturas que le pertenecen a ESTE docente en el periodo activo
            var misAsignaturas = await _context.DocenteAsignaturas
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.AcademicPeriod)
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.Documents) // Para saber si ya subió archivos
                .Where(da => da.DocenteId == currentUser.Id)
                .Select(da => da.Subject)
                .ToListAsync();

            return View(misAsignaturas);
        }
        // GET: Documents/Upload/5 (El ID es el de la asignatura)
        // GET: Documents/Upload/5
        // GET: Documents/Upload/5
        public async Task<IActionResult> Upload(int id, string? faseSeleccionada)
        {
            var subject = await _context.Subjects
                    .Include(s => s.AcademicPeriod)
                    .Include(s => s.Documents!)
                        .ThenInclude(d => d.CutoffDate) // 🟢 ESTA ES LA LÍNEA MÁGICA QUE FALTABA
                    .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            // 1. Buscamos la fase activa real en el calendario
            var faseActiva = await _context.CutoffDates
                .Where(c => c.AcademicPeriodId == subject.AcademicPeriodId && c.DueDate >= DateTime.Now)
                .OrderBy(c => c.DueDate)
                .FirstOrDefaultAsync();

            // 2. Definimos qué fase quiere ver el maestro
            string faseActual = faseSeleccionada ?? faseActiva?.PhaseType ?? "Inicial";

            // 3. 🟢 LÓGICA DE SOLO LECTURA
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
                "Proyecto Individual"
            };
                    break;
                case "Seguimiento1":
                    documentosRequeridos = new List<string> {
                "Avance Apartado 6",
                "Evaluación Diagnóstica"
            };
                    break;
                case "Seguimiento2":
                    documentosRequeridos = new List<string>(); // Vacío
                    break;
                case "Final":
                    documentosRequeridos = new List<string> {
                "Acta de Calificaciones",
                "Instrumentos de Evaluación Finales",
                "Cierre de Proyecto"
            };
                    break;
            }

            ViewBag.FaseActiva = faseActiva;
            ViewBag.FaseActual = faseActual; // La pestaña actual
            ViewBag.EsSoloLectura = esSoloLectura; // Variable para bloquear botones
            ViewBag.DocumentosRequeridos = documentosRequeridos;

            return View(subject);
        }

        // POST: Documents/UploadFile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(int subjectId, int cutoffDateId, string documentType, IFormFile? file, bool isNotApplicable = false)
        {
            // 1. Validar que tengamos un ID de fase válido
            if (cutoffDateId == 0)
            {
                TempData["Error"] = "Error crítico: No hay una Fase Inicial activa para vincular este documento.";
                return RedirectToAction(nameof(Upload), new { id = subjectId });
            }

            var cutoffDate = await _context.CutoffDates.FindAsync(cutoffDateId);
            if (cutoffDate == null) return NotFound();

            string filePath = "N/A";
            DateTime fechaSubida = DateTime.Now;
            bool entregadoATiempo = fechaSubida <= cutoffDate.DueDate;

            // 2. Si NO marcaron la casilla de "N/A", procesamos el PDF obligatoriamente
            if (!isNotApplicable)
            {
                if (file == null || file.Length == 0)
                {
                    TempData["Error"] = "Por favor, selecciona un archivo PDF válido.";
                    return RedirectToAction(nameof(Upload), new { id = subjectId });
                }

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documentos");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                filePath = "/uploads/documentos/" + uniqueFileName;
                string physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }

            // 3. Guardar en la Base de Datos
            var nuevoDocumento = new Document
            {
                SubjectId = subjectId,
                CutoffDateId = cutoffDateId,
                DocumentType = documentType,
                FilePath = filePath,
                UploadedAt = fechaSubida,
                IsOnTime = isNotApplicable || entregadoATiempo, // Si es N/A, cuenta como a tiempo
                Status = isNotApplicable ? "N/A" : (entregadoATiempo ? "EnTiempo" : "Atrasado"),
                ApprovalStatus = "Pendiente"
            };

            _context.Documents.Add(nuevoDocumento);
            await _context.SaveChangesAsync();

            TempData["Success"] = isNotApplicable ? $"El documento '{documentType}' se marcó como No Aplica." : $"Archivo '{documentType}' subido con éxito.";
            return RedirectToAction(nameof(Upload), new { id = subjectId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, int subjectId)
        {
            var doc = await _context.Documents.FindAsync(id);
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

                // Mensaje exacto de tus prototipos
                TempData["Success"] = "ARCHIVO ELIMINADO CON ÉXITO";
            }

            return RedirectToAction(nameof(Upload), new { id = subjectId });
        }
    }

}