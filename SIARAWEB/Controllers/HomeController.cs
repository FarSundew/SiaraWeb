using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // 1. Periodo Académico Activo
            var activePeriod = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive);
            ViewBag.ActivePeriodName = activePeriod?.Name ?? "Sin Periodo Activo";
            ViewBag.UserFullName = string.IsNullOrEmpty(currentUser.FullName) ? currentUser.UserName : currentUser.FullName;

            // 2. Métricas para Docente (o Jefe que imparte materias)
            if (User.IsInRole("Docente") || User.IsInRole("JefeCarrera"))
            {
                var mySubjects = await _context.DocenteAsignaturas
                    .Include(da => da.Subject)
                        .ThenInclude(s => s!.Documents)
                    .Include(da => da.Subject)
                        .ThenInclude(s => s!.AcademicTrackings)
                    .Where(da => da.DocenteId == currentUser.Id)
                    .Select(da => da.Subject)
                    .ToListAsync();

                ViewBag.MySubjectsCount = mySubjects.Count;
                ViewBag.MyDocsCount = mySubjects.Sum(s => s?.Documents?.Count ?? 0);

                var allTrackings = mySubjects.SelectMany(s => s?.AcademicTrackings ?? new List<AcademicTracking>()).ToList();
                ViewBag.MyAvgApproval = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.ApprovalPercentage), 1) : 0;
            }

            // 3. Métricas para Jefes (Carrera o General)
            if (User.IsInRole("JefeCarrera") || User.IsInRole("JefeGeneral"))
            {
                var docentes = await _userManager.GetUsersInRoleAsync("Docente");
                ViewBag.TotalDocentes = docentes.Count;
                ViewBag.TotalAsignaturas = await _context.Subjects.CountAsync();
                ViewBag.TotalDepartamentos = await _context.Departamentos.CountAsync();

                var totalTrackings = await _context.AcademicTrackings.ToListAsync();
                ViewBag.GlobalApproval = totalTrackings.Any() ? Math.Round(totalTrackings.Average(t => t.ApprovalPercentage), 1) : 0;
            }

            // 4. Métricas para Administrador
            if (User.IsInRole("Administrador"))
            {
                ViewBag.TotalUsers = await _context.Users.CountAsync();
            }

            return View();
        }
    }
}