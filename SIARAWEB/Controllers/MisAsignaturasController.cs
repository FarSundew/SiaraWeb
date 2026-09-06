using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data; // Asegúrate de que este namespace sea el correcto para tu proyecto
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    public class MisAsignaturasController : Controller
    {
        // 🟢 CAMBIO AQUÍ: Usamos ApplicationDbContext en lugar de SiaraContext
        private readonly ApplicationDbContext _context;

        // 🟢 INYECCIÓN DE UserManager para obtener el Id del docente (string)
        private readonly UserManager<ApplicationUser> _userManager;

        // 🟢 CAMBIO AQUÍ TAMBIÉN
        public MisAsignaturasController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // El parámetro periodoId nos dirá qué periodo quiere ver el maestro
        public async Task<IActionResult> Index(int? periodoId)
        {
            // Obtener el Id del usuario logueado (string)
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Si no hay usuario logueado devolvemos 403 (puedes cambiar por RedirectToAction("Login") si prefieres)
                return Forbid();
            }
            string docenteId = user.Id;

            // 1. Obtener todos los periodos donde el maestro tiene al menos una materia
            var periodosDelMaestro = await _context.DocenteAsignaturas
                .Include(da => da.Subject)
                    // evitar advertencia de nulabilidad en ThenInclude
                    .ThenInclude(s => s!.AcademicPeriod)
                .Where(da => da.DocenteId == docenteId && da.Subject != null && da.Subject.AcademicPeriod != null)
                .Select(da => da.Subject!.AcademicPeriod!)
                .Distinct()
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            if (!periodosDelMaestro.Any())
            {
                return View(new List<Subject>());
            }

            // 2. Determinar el periodo activo
            var periodoActivo = periodoId.HasValue
                ? periodosDelMaestro.FirstOrDefault(p => p.Id == periodoId.Value)
                : periodosDelMaestro.FirstOrDefault();

            // Nos aseguramos que no sea null (ya comprobamos que hay elementos)
            periodoActivo ??= periodosDelMaestro.First();

            // 3. Lógica para las flechas
            int currentIndex = periodosDelMaestro.IndexOf(periodoActivo);
            ViewBag.SiguientePeriodo = currentIndex > 0 ? periodosDelMaestro[currentIndex - 1] : null;
            ViewBag.AnteriorPeriodo = currentIndex < periodosDelMaestro.Count - 1 ? periodosDelMaestro[currentIndex + 1] : null;

            // Evitar la advertencia CS8619 asignando como object (dynamic acepta object sin conflicto de nulabilidad)
            ViewBag.PeriodoActivo = (object)periodoActivo;

            // 4. Buscar las materias SOLO de ese periodo activo
            var materias = await _context.DocenteAsignaturas
                .Include(da => da.Subject)
                .Where(da => da.DocenteId == docenteId && da.Subject != null && da.Subject.AcademicPeriodId == periodoActivo.Id)
                .Select(da => da.Subject!)
                .ToListAsync();

            // 5. Buscar si hay una fecha de corte activa para ese periodo
            ViewBag.ProximoCorte = await _context.CutoffDates
                .Where(c => c.AcademicPeriodId == periodoActivo.Id && c.DueDate >= DateTime.Now)
                .OrderBy(c => c.DueDate)
                .FirstOrDefaultAsync();

            return View(materias);
        }
    }
}