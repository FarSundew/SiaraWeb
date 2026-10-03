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

        // GET: CutoffDates/Edit/5
        [Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Leer directamente sin tracking para eliminar efectos secundarios de EF
            var cutoffDate = await _context.CutoffDates
                .AsNoTracking()
                .Include(c => c.AcademicPeriod)
                .Include(c => c.Departamento)
                .FirstOrDefaultAsync(c => c.Id == id.Value);

            if (cutoffDate == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                if (cutoffDate.DepartamentoId.HasValue && cutoffDate.DepartamentoId != currentUser?.DepartamentoId)
                {
                    return Forbid();
                }
            }

            ViewBag.AcademicPeriodId = new SelectList(_context.AcademicPeriods.OrderByDescending(p => p.Id), "Id", "Name", cutoffDate.AcademicPeriodId);
            ViewBag.DepartamentoId = new SelectList(_context.Departamentos.OrderBy(d => d.Name), "Id", "Name", cutoffDate.DepartamentoId);

            // Eliminar cualquier entrada previa en ModelState que pueda forzar valores vacíos en los tag-helpers
            ModelState.Remove("Name");
            ModelState.Remove("StartDate");
            ModelState.Remove("DueDate");
            // Medida extra por si persiste: limpiar todo ModelState
            // ModelState.Clear();

            return View(cutoffDate);
        }

        // POST: CutoffDates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]
        public async Task<IActionResult> Edit(int id, CutoffDate cutoffDate)
        {
            if (id != cutoffDate.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cutoffDate);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"La fecha de corte '{cutoffDate.Name}' se actualizó correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.CutoffDates.Any(e => e.Id == cutoffDate.Id)) return NotFound();
                    else throw;
                }
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