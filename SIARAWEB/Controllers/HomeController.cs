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
            var activePeriod = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive)
                               ?? await _context.AcademicPeriods.OrderByDescending(p => p.Id).FirstOrDefaultAsync();

            int activePeriodId = activePeriod?.Id ?? 0;
            ViewBag.ActivePeriodName = activePeriod?.Name ?? "Sin Periodo Activo";
            ViewBag.UserFullName = string.IsNullOrEmpty(currentUser.FullName) ? currentUser.UserName : currentUser.FullName;

            // 2. Métricas para Docente (o Jefe de Carrera que imparte cátedra)
            if (User.IsInRole("Docente") || User.IsInRole("JefeCarrera"))
            {
                var mySubjects = await _context.DocenteAsignaturas
                    .Include(da => da.Subject)
                        .ThenInclude(s => s!.Documents)
                    .Include(da => da.Subject)
                        .ThenInclude(s => s!.AcademicTrackings)
                    .Where(da => da.DocenteId == currentUser.Id &&
                                 da.Subject != null &&
                                 da.Subject.AcademicPeriodId == activePeriodId)
                    .Select(da => da.Subject!)
                    .ToListAsync();

                ViewBag.MySubjectsCount = mySubjects.Count;
                ViewBag.MyDocsCount = mySubjects.Sum(s => s.Documents?.Count ?? 0);

                var allTrackings = mySubjects
                    .SelectMany(s => s.AcademicTrackings ?? new List<AcademicTracking>())
                    .ToList();

                ViewBag.MyAvgApproval = allTrackings.Any()
                    ? Math.Round(allTrackings.Average(t => t.ApprovalPercentage), 1)
                    : 0;
            }

            // 3. Indicadores de Dirección Académica (Jefe de Carrera vs. Jefe General)
            if (User.IsInRole("JefeCarrera") || User.IsInRole("JefeGeneral"))
            {
                bool esJefeCarrera = User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador");
                int deptoId = currentUser.DepartamentoId ?? 0;

                if (esJefeCarrera)
                {
                    // 🔒 CONSULTAS LIMITADAS A SU CARRERA
                    var depto = await _context.Departamentos.FindAsync(deptoId);
                    ViewBag.NombreCarrera = depto?.Code ?? depto?.Name ?? "Mi Carrera";

                    // Docentes: Adscritos a su academia O docentes de apoyo con materias en su departamento
                    var docentesIds = (await _userManager.GetUsersInRoleAsync("Docente")).Select(d => d.Id).ToList();
                    var totalDocentesCarrera = await _context.Users
                        .Where(u => docentesIds.Contains(u.Id) &&
                                   (u.DepartamentoId == deptoId ||
                                    u.DocenteAsignaturas!.Any(da => da.Subject != null &&
                                                                   da.Subject.DepartamentoId == deptoId &&
                                                                   da.Subject.AcademicPeriodId == activePeriodId)))
                        .CountAsync();
                    ViewBag.TotalDocentes = totalDocentesCarrera;

                    // Asignaturas de su carrera en el periodo activo
                    var totalAsignaturasCarrera = await _context.Subjects
                        .Where(s => s.DepartamentoId == deptoId && s.AcademicPeriodId == activePeriodId)
                        .CountAsync();
                    ViewBag.TotalAsignaturas = totalAsignaturasCarrera;

                    // Carreras a su cargo
                    ViewBag.TotalDepartamentos = 1;

                    // Acreditación promedio de su carrera en el periodo activo
                    var trackingsCarrera = await _context.AcademicTrackings
                        .Include(t => t.Subject)
                        .Where(t => t.Subject != null &&
                                    t.Subject.DepartamentoId == deptoId &&
                                    t.Subject.AcademicPeriodId == activePeriodId)
                        .ToListAsync();

                    ViewBag.GlobalApproval = trackingsCarrera.Any()
                        ? Math.Round(trackingsCarrera.Average(t => t.ApprovalPercentage), 1)
                        : 0;
                }
                else
                {
                    // 🌐 CONSULTA INSTITUCIONAL GLOBAL (Jefe General / Subdirector)
                    ViewBag.NombreCarrera = "Todas las Carreras";
                    ViewBag.TotalDocentes = (await _userManager.GetUsersInRoleAsync("Docente")).Count;
                    ViewBag.TotalAsignaturas = await _context.Subjects
                        .Where(s => s.AcademicPeriodId == activePeriodId)
                        .CountAsync();
                    ViewBag.TotalDepartamentos = await _context.Departamentos.CountAsync();

                    var totalTrackings = await _context.AcademicTrackings
                        .Include(t => t.Subject)
                        .Where(t => t.Subject != null && t.Subject.AcademicPeriodId == activePeriodId)
                        .ToListAsync();

                    ViewBag.GlobalApproval = totalTrackings.Any()
                        ? Math.Round(totalTrackings.Average(t => t.ApprovalPercentage), 1)
                        : 0;
                }
            }

            // 4. Métricas para Administrador (TI)
            if (User.IsInRole("Administrador"))
            {
                ViewBag.TotalUsers = await _context.Users.CountAsync();
            }

            return View();
        }
    }
}