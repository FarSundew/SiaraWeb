using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "JefeGeneral,Administrador")]
    public class CutoffDatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CutoffDatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CutoffDates
        public async Task<IActionResult> Index()
        {
            var dates = await _context.CutoffDates
                                      .Include(c => c.AcademicPeriod)
                                      .OrderByDescending(c => c.DueDate)
                                      .ToListAsync();
            return View(dates);
        }

        // GET: CutoffDates/Create
        public IActionResult Create()
        {
            ViewBag.AcademicPeriods = new SelectList(_context.AcademicPeriods.Where(p => p.IsActive), "Id", "Name");
            return View();
        }

        // POST: CutoffDates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CutoffDate cutoffDate)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cutoffDate);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Fecha de corte programada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.AcademicPeriods = new SelectList(_context.AcademicPeriods.Where(p => p.IsActive), "Id", "Name", cutoffDate.AcademicPeriodId);
            return View(cutoffDate);
        }

        // POST: CutoffDates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cutoffDate = await _context.CutoffDates.FindAsync(id);
            if (cutoffDate != null)
            {
                try
                {
                    _context.CutoffDates.Remove(cutoffDate);
                    await _context.SaveChangesAsync(); // Aquí es donde ocurría el error original
                    TempData["Success"] = "Fase eliminada correctamente.";
                }
                catch (DbUpdateException)
                {
                    // 🟢 ATRAPAMOS EL ERROR DE LLAVE FORÁNEA
                    TempData["Error"] = "No puedes eliminar esta fase porque ya existen documentos o calificaciones vinculadas a ella. Primero debes eliminar esos registros.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}