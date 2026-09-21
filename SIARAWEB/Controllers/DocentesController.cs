using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    // 🔒 Acceso para el Jefe de Carrera, Jefe General y Soporte Técnico
    [Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]
    public class DocentesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DocentesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Docentes
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // 1. Identificar a todos los usuarios con rol "Docente"
            var docentesList = await _userManager.GetUsersInRoleAsync("Docente");
            var docentesIds = docentesList.Select(d => d.Id).ToList();

            // 2. Consulta base incluyendo departamento y asignaciones
            var query = _context.Users
                .Where(u => docentesIds.Contains(u.Id))
                .Include(u => u.Departamento)
                .Include(u => u.DocenteAsignaturas!)
                    .ThenInclude(da => da.Subject)
                        .ThenInclude(s => s!.AcademicPeriod)
                .AsQueryable();

            // 🔒 Si es Jefe de Carrera: Muestra los adscritos a su carrera Y los que imparten materias de su carrera
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                int deptoId = currentUser.DepartamentoId ?? 0;
                query = query.Where(u => u.DepartamentoId == deptoId ||
                                         u.DocenteAsignaturas!.Any(da => da.Subject != null && da.Subject.DepartamentoId == deptoId));
            }

            var docentesConMaterias = await query
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.MiDepartamentoId = currentUser.DepartamentoId ?? 0;

            return View(docentesConMaterias);
        }

        // GET: Docentes/Details/5
        public async Task<IActionResult> Details(string id, int page = 1)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // 🟢 1. CARGA EXPLÍCITA DEL DEPARTAMENTO (Obligatorio)
            var docente = await _context.Users
                .Include(u => u.Departamento)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (docente == null) return NotFound();

            // 🟢 2. PASAR EL NOMBRE DEL DEPARTAMENTO AL VIEWBAG
            ViewBag.DocenteDepto = docente.Departamento?.Name ?? "Sin Departamento Base";
            ViewBag.DocenteNombre = docente.FullName;
            ViewBag.DocenteEmail = docente.Email;
            ViewBag.DocenteId = docente.Id;

            int pageSize = 6;

            // 3. Consulta de sus materias asignadas
            var query = _context.DocenteAsignaturas
                .Include(da => da.Subject)
                    .ThenInclude(s => s!.Departamento)
                .Include(da => da.AcademicPeriod)
                .Where(da => da.DocenteId == id)
                .AsQueryable();

            int totalRegistros = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var asignacionesPaginadas = await query
                .OrderByDescending(da => da.AcademicPeriod != null && da.AcademicPeriod.IsActive)
                .ThenByDescending(da => da.AcademicPeriodId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var agrupado = asignacionesPaginadas
                .GroupBy(da => da.AcademicPeriod!)
                .OrderByDescending(g => g.Key != null && g.Key.IsActive)
                .ThenByDescending(g => g.Key != null ? g.Key.Id : 0)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalRegistros;

            return View(agrupado);
        }

        // POST: Docentes/ToggleReviewerRole/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera")]
        public async Task<IActionResult> ToggleReviewerRole(string docenteId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var docente = await _userManager.FindByIdAsync(docenteId);
            if (docente == null)
            {
                TempData["Error"] = "Docente no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // 🔒 REGLA DE SEGURIDAD ESTRICTA:
            // Solo se puede comisionar a docentes ADSCRITOS a la carrera del Jefe autenticado
            if (docente.DepartamentoId != currentUser.DepartamentoId)
            {
                TempData["Error"] = "Operación denegada: No puedes otorgar permisos de revisor auxiliar a un docente adscrito a otra carrera.";
                return RedirectToAction(nameof(Index));
            }

            bool isReviewer = await _userManager.IsInRoleAsync(docente, "JefeCarrera");

            if (isReviewer)
            {
                await _userManager.RemoveFromRoleAsync(docente, "JefeCarrera");
                TempData["Success"] = $"Se retiraron los permisos de revisión al docente {docente.FullName}.";
            }
            else
            {
                await _userManager.AddToRoleAsync(docente, "JefeCarrera");
                TempData["Success"] = $"El docente {docente.FullName} fue comisionado como revisor auxiliar de la academia.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}