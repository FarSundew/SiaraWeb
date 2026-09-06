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

        // GET: AcademicTrackings (Lista de Materias del Docente para Seguimiento)
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var misAsignaturas = await _context.DocenteAsignaturas
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.AcademicPeriod)
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.AcademicTrackings)
                .Where(da => da.DocenteId == currentUser.Id)
                .Select(da => da.Subject)
                .ToListAsync();

            return View(misAsignaturas);
        }

        // GET: AcademicTrackings/Capture/5 (ID de la Asignatura)
        public async Task<IActionResult> Capture(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.AcademicPeriod)
                .Include(s => s.AcademicTrackings!)
                    .ThenInclude(at => at.CutoffDate)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            ViewBag.CutoffDates = new SelectList(
                _context.CutoffDates.Where(c => c.AcademicPeriodId == subject.AcademicPeriodId && c.PhaseType != "Inicial"),                "Id",
                "Name"
            );

            ViewBag.Subject = subject;
            return View(new AcademicTracking { SubjectId = id });
        }

        // POST: AcademicTrackings/Capture
        [HttpPost]
        [ValidateAntiForgeryToken]
        // GET: AcademicTrackings/Capture/5
        public async Task<IActionResult> Capture(int id, string? faseSeleccionada)
        {
            var subject = await _context.Subjects
                .Include(s => s.AcademicPeriod)
                .Include(s => s.AcademicTrackings) // Traemos las calificaciones guardadas
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            // 1. Buscamos la fase activa real en el calendario
            var faseActiva = await _context.CutoffDates
                .Where(c => c.AcademicPeriodId == subject.AcademicPeriodId && c.DueDate >= DateTime.Now)
                .OrderBy(c => c.DueDate)
                .FirstOrDefaultAsync();

            // 2. Definimos qué fase se va a mostrar en pantalla
            // Si el maestro no seleccionó ninguna, mostramos la activa. Si no hay activa, por defecto "Seguimiento1"
            string faseActual = faseSeleccionada ?? faseActiva?.PhaseType ?? "Seguimiento1";

            // 3. 🟢 LÓGICA DE SOLO LECTURA: 
            // Es de solo lectura si no hay fase activa, O si la fase que está viendo NO es la activa
            bool esSoloLectura = faseActiva == null || faseActiva.PhaseType != faseActual;

            // Pasamos todas estas variables a la vista
            ViewBag.FaseActiva = faseActiva?.PhaseType; // Para saber cuál pintar de verde
            ViewBag.FaseActual = faseActual; // La que estamos viendo ahorita
            ViewBag.EsSoloLectura = esSoloLectura;

            return View(subject);
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