using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    [Authorize]
    public class MisAsignaturasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MisAsignaturasController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: MisAsignaturas
        public async Task<IActionResult> Index(int? periodoId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();
            string docenteId = user.Id;

            // 1. Obtener los periodos escolares donde el docente tiene materias registradas
            var periodosDelMaestro = await _context.DocenteAsignaturas
                .Include(da => da.AcademicPeriod)
                .Where(da => da.DocenteId == docenteId && da.AcademicPeriod != null)
                .Select(da => da.AcademicPeriod!)
                .Distinct()
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            if (!periodosDelMaestro.Any())
            {
                // Si aún no tiene periodos con carga, busca el periodo escolar activo general
                var periodoVigente = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive);
                ViewBag.PeriodoActivo = periodoVigente;
                return View(new List<Subject>());
            }

            // 2. Determinar el periodo seleccionado o el activo por defecto
            var periodoActivo = periodoId.HasValue
                ? periodosDelMaestro.FirstOrDefault(p => p.Id == periodoId.Value)
                : periodosDelMaestro.FirstOrDefault(p => p.IsActive) ?? periodosDelMaestro.First();

            periodoActivo ??= periodosDelMaestro.First();

            // 3. Navegación entre semestres (flechas)
            int currentIndex = periodosDelMaestro.IndexOf(periodoActivo);
            ViewBag.SiguientePeriodo = currentIndex > 0 ? periodosDelMaestro[currentIndex - 1] : null;
            ViewBag.AnteriorPeriodo = currentIndex < periodosDelMaestro.Count - 1 ? periodosDelMaestro[currentIndex + 1] : null;
            ViewBag.PeriodoActivo = periodoActivo;

            // 4. Obtener las materias asignadas al docente en el periodo activo
            var materias = await _context.DocenteAsignaturas
                .Include(da => da.Subject)
                .Where(da => da.DocenteId == docenteId &&
                             da.AcademicPeriodId == periodoActivo.Id &&
                             da.Subject != null)
                .Select(da => da.Subject!)
                .Distinct()
                .ToListAsync();

            // 5. Fecha de corte próxima para este ciclo escolar
            ViewBag.ProximoCorte = await _context.CutoffDates
                .Where(c => c.AcademicPeriodId == periodoActivo.Id && c.DueDate >= DateTime.Now)
                .OrderBy(c => c.DueDate)
                .FirstOrDefaultAsync();

            return View(materias);
        }
    }
}