using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data; // Ajusta este namespace al de tu proyecto si es distinto
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Obtenemos todas las materias con sus documentos, fechas de corte y docentes asignados
            var asignaturas = await _context.Subjects
                .Include(s => s.DocenteAsignaturas!).ThenInclude(da => da.Docente)
                .Include(s => s.Documents!).ThenInclude(d => d.CutoffDate)
                .ToListAsync();

            var listaSemaforo = new List<SemaforoViewModel>();

            foreach (var materia in asignaturas)
            {
                var item = new SemaforoViewModel
                {
                    SubjectId = materia.Id,
                    SubjectCode = materia.Code,
                    SubjectName = materia.Name,
                    DocenteName = materia.DocenteAsignaturas?.FirstOrDefault()?.Docente?.FullName ?? "Sin asignar"
                };

                // 2. Función local para evaluar el color del semáforo por cada fase
                string EvaluarSemaforo(string phaseType)
                {
                    var docsFase = materia.Documents?.Where(d => d.CutoffDate?.PhaseType == phaseType).ToList();

                    if (docsFase == null || !docsFase.Any()) return "bg-secondary"; // Gris: Pendiente

                    // Si al menos un documento se entregó con retraso, se pinta rojo
                    bool tieneAtraso = docsFase.Any(d => !d.IsOnTime);
                    return tieneAtraso ? "bg-danger" : "bg-success";
                }

                // 3. Asignamos los colores evaluando automáticamente las 4 fases
                item.ColorInicial = EvaluarSemaforo("Inicial");
                item.ColorSeg1 = EvaluarSemaforo("Seguimiento1");
                item.ColorSeg2 = EvaluarSemaforo("Seguimiento2");
                item.ColorFinal = EvaluarSemaforo("Final");

                listaSemaforo.Add(item);
            }

            return View(listaSemaforo);
            // Estadísticas simuladas para la Gráfica de Pastel (Rendimiento)
            ViewBag.Aprobacion = 75;
            ViewBag.Reprobacion = 15;
            ViewBag.Desercion = 10;

            // Estadísticas REALES calculadas desde tu semáforo para la Gráfica de Barras
            ViewBag.DocsATiempo = listaSemaforo.Count(s => s.ColorInicial == "bg-success");
            ViewBag.DocsDesfasados = listaSemaforo.Count(s => s.ColorInicial == "bg-danger");
            ViewBag.DocsPendientes = listaSemaforo.Count(s => s.ColorInicial == "bg-secondary");
        }

        // GET: Reports/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var materia = await _context.Subjects
                .Include(s => s.DocenteAsignaturas!).ThenInclude(da => da.Docente)
                .Include(s => s.Documents!).ThenInclude(d => d.CutoffDate)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (materia == null)
            {
                return NotFound();
            }

            return View(materia);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewDocument(int documentId, int subjectId, string? feedback, string status)
        {
            // 1. Buscamos el documento exacto en la base de datos
            var document = await _context.Documents.FindAsync(documentId);

            if (document != null)
            {
                // 2. Actualizamos el estado (Aprobado o Rechazado)
                document.ApprovalStatus = status;

                // 3. Guardamos la retroalimentación (o un texto vacío si no escribieron nada)
                document.Feedback = feedback ?? string.Empty;

                _context.Documents.Update(document);
                await _context.SaveChangesAsync();

                // Mensaje de éxito que tu vista ya está configurada para mostrar
                TempData["Success"] = $"El documento fue {status.ToUpper()} con éxito.";
            }
            else
            {
                TempData["Error"] = "Hubo un problema al encontrar el documento.";
            }

            // 4. Redirigimos de vuelta a la misma pantalla de auditoría para ver los cambios
            return RedirectToAction(nameof(Details), new { id = subjectId });
        }
    }
}