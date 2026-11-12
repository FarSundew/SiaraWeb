using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    // 🔒 MÓDULO EXCLUSIVO DEL ADMINISTRADOR TÉCNICO (TI)
    [Authorize(Roles = "Administrador")]
    public class AcademicPeriodsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AcademicPeriodsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AcademicPeriods
        public IActionResult Index()
        {
            var config = PeriodConfig.Load();
            return View(config);
        }

        // POST: AcademicPeriods/UpdateNames
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNames(string periodo1, string periodo2, string? periodoExtra)
        {
            if (string.IsNullOrWhiteSpace(periodo1) || string.IsNullOrWhiteSpace(periodo2))
            {
                TempData["Error"] = "Los nombres de los dos periodos principales son obligatorios.";
                return RedirectToAction(nameof(Index));
            }

            var oldConfig = PeriodConfig.Load();
            string oldP1 = oldConfig.Periodo1.Trim();
            string oldP2 = oldConfig.Periodo2.Trim();

            var newConfig = new PeriodConfig
            {
                Periodo1 = periodo1.Trim(),
                Periodo2 = periodo2.Trim(),
                PeriodoExtra = periodoExtra?.Trim() ?? ""
            };
            newConfig.Save();

            // También actualizar los registros existentes en la BD si cambiaron de nombre (ej. de "Julio - Diciembre 2026" a "Agosto - Diciembre 2026")
            var periodosEnBd = await _context.AcademicPeriods.ToListAsync();
            foreach (var p in periodosEnBd)
            {
                if (!string.IsNullOrEmpty(oldP1) && oldP1 != newConfig.Periodo1 && p.Name.StartsWith(oldP1))
                {
                    p.Name = newConfig.Periodo1 + p.Name.Substring(oldP1.Length);
                }
                else if (!string.IsNullOrEmpty(oldP2) && oldP2 != newConfig.Periodo2 && p.Name.StartsWith(oldP2))
                {
                    p.Name = newConfig.Periodo2 + p.Name.Substring(oldP2.Length);
                }
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Los nombres del selector de periodos se actualizaron a '{newConfig.Periodo1}' y '{newConfig.Periodo2}' en todo el sistema.";
            return RedirectToAction(nameof(Index));
        }
    }
}
