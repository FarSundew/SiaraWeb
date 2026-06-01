using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;

namespace SIARAWEB.Controllers
{
    // 🔒 BLOQUEO: Solo los Jefes de Carrera pueden ver las gráficas y reportes
    [Authorize(Roles = "Administrador")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports (Pantalla del Dashboard de Gráficas)
        public async Task<IActionResult> Index()
        {
            // Consultamos todos los seguimientos que los maestros han capturado
            var trackings = await _context.AcademicTrackings.ToListAsync();

            // Calculamos los promedios generales para las gráficas
            if (trackings.Any())
            {
                ViewBag.IndiceAprobacion = trackings.Average(t => t.ApprovalPercentage);
                ViewBag.IndiceReprobacion = trackings.Average(t => t.FailurePercentage);
                ViewBag.TasaDesercion = trackings.Average(t => t.DropoutPercentage);
            }
            else
            {
                // Si el semestre va empezando y no hay datos, enviamos 0 para no romper la gráfica
                ViewBag.IndiceAprobacion = 0;
                ViewBag.IndiceReprobacion = 0;
                ViewBag.TasaDesercion = 0;
            }

            return View();
        }
    }
}