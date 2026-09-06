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
            // 1. Obtener todos los usuarios con el rol de "Docente"
            var docentesList = await _userManager.GetUsersInRoleAsync("Docente");
            var docentesIds = docentesList.Select(d => d.Id).ToList();

            // 2. Traer la información completa desde la base de datos incluyendo las asignaturas asignadas
            var docentesConMaterias = await _context.Users
                .Where(u => docentesIds.Contains(u.Id))
                .Include(u => u.DocenteAsignaturas!)
                    .ThenInclude(da => da.Subject)
                        .ThenInclude(s => s!.AcademicPeriod)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(docentesConMaterias);
        }

        // GET: Docentes/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var docente = await _context.Users
                .Include(u => u.DocenteAsignaturas!)
                    .ThenInclude(da => da.Subject)
                        .ThenInclude(s => s!.AcademicPeriod)
                .Include(u => u.DocenteAsignaturas!)
                    .ThenInclude(da => da.Subject)
                        .ThenInclude(s => s!.Departamento)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (docente == null)
            {
                return NotFound();
            }

            return View(docente);
        }
    }
}