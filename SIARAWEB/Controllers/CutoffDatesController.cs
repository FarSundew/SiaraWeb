using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "JefeCarrera,Administrador")]
    public class CutoffDatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CutoffDatesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: CutoffDates
        public async Task<IActionResult> Index(int? departamentoId, int? periodoId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var periodos = await _context.AcademicPeriods.OrderByDescending(p => p.Id).ToListAsync();
            int selectedPeriodId = periodoId ?? periodos.FirstOrDefault()?.Id ?? 0;

            var query = _context.CutoffDates
                .Include(c => c.AcademicPeriod)
                .Include(c => c.Departamento)
                .Where(c => c.AcademicPeriodId == selectedPeriodId)
                .AsQueryable();

            // Si es Jefe de Carrera, se filtra por su propio departamento
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                query = query.Where(c => c.DepartamentoId == currentUser.DepartamentoId || c.DepartamentoId == null);
            }
            else if (departamentoId.HasValue && departamentoId.Value > 0)
            {
                query = query.Where(c => c.DepartamentoId == departamentoId.Value);
            }

            ViewBag.Periodos = new SelectList(periodos, "Id", "Name", selectedPeriodId);
            ViewBag.Departamentos = new SelectList(await _context.Departamentos.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", departamentoId);

            return View(await query.OrderBy(c => c.StartDate).ToListAsync());
        }

        // GET: CutoffDates/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            ViewBag.AcademicPeriodId = new SelectList(
                await _context.AcademicPeriods.OrderByDescending(p => p.Id).ToListAsync(),
                "Id", "Name");

            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                ViewBag.DepartamentoId = new SelectList(
                    await _context.Departamentos.Where(d => d.Id == currentUser!.DepartamentoId).ToListAsync(),
                    "Id", "Name", currentUser?.DepartamentoId);
            }
            else
            {
                ViewBag.DepartamentoId = new SelectList(
                    await _context.Departamentos.OrderBy(d => d.Name).ToListAsync(),
                    "Id", "Name");
            }

            return View();
        }

        // POST: CutoffDates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CutoffDate cutoffDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Si es jefe de carrera, forzar su propio departamento
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                cutoffDate.DepartamentoId = currentUser?.DepartamentoId;
            }

            if (ModelState.IsValid)
            {
                _context.CutoffDates.Add(cutoffDate);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Fecha de corte '{cutoffDate.Name}' creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AcademicPeriodId = new SelectList(_context.AcademicPeriods, "Id", "Name", cutoffDate.AcademicPeriodId);
            ViewBag.DepartamentoId = new SelectList(_context.Departamentos, "Id", "Name", cutoffDate.DepartamentoId);
            return View(cutoffDate);
        }

        // POST: CutoffDates/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cutoff = await _context.CutoffDates.FindAsync(id);
            if (cutoff != null)
            {
                _context.CutoffDates.Remove(cutoff);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Fecha de corte eliminada.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}