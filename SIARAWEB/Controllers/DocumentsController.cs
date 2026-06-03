using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SIARAWEB.Controllers
{
    // Solo pedimos que el usuario haya iniciado sesión (sin especificar rol estricto)
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        public DocumentsController(ApplicationDbContext context,
                                   IWebHostEnvironment webHostEnvironment,
                                   UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        // GET: Documents
        public async Task<IActionResult> Index()
        {
            // Consultamos la fecha límite para saber en qué seguimiento estamos
            var fechaCorte = await _context.TrackingDeadline
                .Where(f => f.CutoffDate >= DateTime.Today)
                .OrderBy(f => f.CutoffDate)
                .FirstOrDefaultAsync();

            ViewBag.FechaCorte = fechaCorte;

            var documentos = _context.Documents.Include(d => d.Subject).AsQueryable();

            // Si es Administrador, ve la tabla general de monitoreo
            if (User.IsInRole("Administrador"))
            {
                return View(await documentos.ToListAsync());
            }
            else
            {
                // Si NO es administrador, asumimos que es el Docente
                var userId = _userManager.GetUserId(User);

                // Buscamos qué materias tiene asignadas este maestro
                var misMateriasIds = await _context.DocenteAsignaturas
                    .Where(da => da.DocenteId == userId)
                    .Select(da => da.SubjectId)
                    .ToListAsync();

                // Extraemos las materias completas para dibujar la cuadrícula en la vista
                ViewBag.MisMaterias = await _context.Subjects
                    .Where(s => misMateriasIds.Contains(s.Id))
                    .ToListAsync();

                // Filtramos para enviar solo los documentos que le pertenecen a este maestro
                var misDocumentos = await documentos
                    .Where(d => misMateriasIds.Contains(d.SubjectId))
                    .ToListAsync();

                return View(misDocumentos);
            }
        }

        // GET: Documents/Create
        // Ahora recibe el id de la materia y el tipo de documento desde el botón de la cuadrícula
        public async Task<IActionResult> Create(int? subjectId, string tipo)
        {
            // Si alguien intenta entrar directo por URL, lo regresamos al índice
            if (subjectId == null || string.IsNullOrEmpty(tipo))
            {
                return RedirectToAction(nameof(Index));
            }

            var fechaCorte = await _context.TrackingDeadline
                .Where(f => f.CutoffDate >= DateTime.Today)
                .OrderBy(f => f.CutoffDate)
                .FirstOrDefaultAsync();

            ViewBag.FechaCorte = fechaCorte;

            // Buscamos la materia para mostrarle al docente a qué materia le está subiendo
            var materia = await _context.Subjects.FindAsync(subjectId);
            if (materia == null) return NotFound();

            ViewBag.MateriaNombre = materia.Name;

            // Creamos un documento base con los datos ya llenos para mandárselos a la vista
            var document = new Document
            {
                SubjectId = subjectId.Value,
                Type = tipo
            };

            return View(document);
        }

        // POST: Documents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SubjectId,Type")] Document document, IFormFile archivoPdf)
        {
            // Ignoramos el objeto Subject para que no falle la validación del modelo
            ModelState.Remove("Subject");
            ModelState.Remove("FilePath");

            var fechaCorte = await _context.TrackingDeadline
                .Where(f => f.CutoffDate >= DateTime.Today)
                .OrderBy(f => f.CutoffDate)
                .FirstOrDefaultAsync();

            if (ModelState.IsValid)
            {
                if (archivoPdf != null && archivoPdf.Length > 0)
                {
                    // Guardado físico del archivo
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + archivoPdf.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await archivoPdf.CopyToAsync(fileStream);
                    }

                    // Llenar los datos internos de la base de datos
                    document.FilePath = "/uploads/" + uniqueFileName;
                    document.UploadedAt = DateTime.Now;

                    if (fechaCorte != null)
                    {
                        // Compara la fecha de entrega con la fecha límite
                        document.IsOnTime = document.UploadedAt.Date <= fechaCorte.CutoffDate.Date;
                    }
                    else
                    {
                        document.IsOnTime = true;
                    }

                    _context.Add(document);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Por favor adjunta un documento PDF válido.");
                }
            }

            // SI LA VALIDACIÓN FALLA: Recargamos los textos para que la pantalla no truene
            ViewBag.FechaCorte = fechaCorte;
            var materiaFallo = await _context.Subjects.FindAsync(document.SubjectId);
            ViewBag.MateriaNombre = materiaFallo?.Name;

            return View(document);
        }

        // GET: Documents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.Documents
                .Include(d => d.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (document == null) return NotFound();

            return View(document);
        }

        // GET: Documents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.Documents
                .Include(d => d.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (document == null) return NotFound();

            return View(document);
        }

        // POST: Documents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document != null)
            {
                // Elimina físicamente el archivo del servidor
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Lo elimina de la base de datos
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}