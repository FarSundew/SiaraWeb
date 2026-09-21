using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    [Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]
    public class SubjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Subjects
        public async Task<IActionResult> Index(int? periodoId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // 1. Determinar el periodo activo o el más reciente
            var periodoActivo = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive)
                                ?? await _context.AcademicPeriods.OrderByDescending(p => p.Id).FirstOrDefaultAsync();

            int activePeriodId = periodoId ?? periodoActivo?.Id ?? 0;

            // 2. Consulta del catálogo maestro de asignaturas
            var query = _context.Subjects
                .Include(s => s.Departamento)
                .Include(s => s.DocenteAsignaturas!)
                    .ThenInclude(da => da.Docente)
                .Include(s => s.DocenteAsignaturas!)
                    .ThenInclude(da => da.AcademicPeriod)
                .AsQueryable();

            // 🔒 REGLA MULTI-TENANT: El Jefe de Carrera solo ve el catálogo de su carrera
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                query = query.Where(s => s.DepartamentoId == currentUser.DepartamentoId);
            }

            ViewBag.Periodos = await _context.AcademicPeriods.OrderByDescending(p => p.Id).ToListAsync();
            ViewBag.PeriodoSeleccionado = activePeriodId;
            ViewBag.ActivePeriodName = periodoActivo?.Name ?? "Sin Periodo Activo";
            ViewBag.EsPeriodoActivo = periodoActivo != null && periodoActivo.Id == activePeriodId && periodoActivo.IsActive;

            // Cargar la lista unificada de docentes para el modal de asignación rápida
            ViewBag.Docentes = new SelectList(await ObtenerDocentesDisponibles(), "Id", "FullName");

            return View(await query.OrderBy(s => s.Name).ToListAsync());
        }

        // GET: Subjects/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // Cargar todos los periodos (o al menos los activos si existen)
            var periodos = await _context.AcademicPeriods.OrderByDescending(p => p.Id).ToListAsync();

            // Si quieres mostrar solo activos pero no hay ninguno, muestra todos para no romper la vista:
            var periodosDisponibles = periodos.Any(p => p.IsActive)
                ? periodos.Where(p => p.IsActive).ToList()
                : periodos;

            ViewBag.AcademicPeriods = new SelectList(periodosDisponibles, "Id", "Name");

            // Departamentos
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                ViewBag.Departamentos = new SelectList(
                    await _context.Departamentos.Where(d => d.Id == currentUser.DepartamentoId).ToListAsync(),
                    "Id", "Name", currentUser.DepartamentoId);
            }
            else
            {
                ViewBag.Departamentos = new SelectList(
                    await _context.Departamentos.OrderBy(d => d.Name).ToListAsync(),
                    "Id", "Name");
            }

            return View();
        }

        // POST: Subjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Subject subject)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // 🔒 Forzar el departamento del usuario si es Jefe de Carrera
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                if (!currentUser.DepartamentoId.HasValue)
                {
                    TempData["Error"] = "Tu cuenta de usuario no tiene un departamento asociado.";
                    return RedirectToAction(nameof(Index));
                }
                subject.DepartamentoId = currentUser.DepartamentoId.Value;
            }

            if (ModelState.IsValid)
            {
                subject.Code = subject.Code?.Trim().ToUpper() ?? string.Empty;

                _context.Add(subject);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Asignatura registrada exitosamente en el catálogo maestro.";
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                ViewBag.Departamentos = new SelectList(
                    await _context.Departamentos.Where(d => d.Id == currentUser.DepartamentoId).ToListAsync(),
                    "Id", "Name", currentUser.DepartamentoId);
            }
            else
            {
                ViewBag.Departamentos = new SelectList(
                    await _context.Departamentos.OrderBy(d => d.Name).ToListAsync(),
                    "Id", "Name", subject.DepartamentoId);
            }

            return View(subject);
        }

        // POST: Subjects/AssignTeacher
        // Vincula un docente a un grupo/turno para la materia en el periodo indicado
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera,Administrador")]
        public async Task<IActionResult> AssignTeacher(int subjectId, int periodId, string docenteId, string group)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var subject = await _context.Subjects.FindAsync(subjectId);
            if (subject == null) return NotFound();

            // 🔒 Validación de departamento
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                if (subject.DepartamentoId != currentUser.DepartamentoId) return Forbid();
            }

            if (string.IsNullOrWhiteSpace(docenteId))
            {
                TempData["Error"] = "Debes seleccionar un docente titular.";
                return RedirectToAction(nameof(Index), new { periodoId = periodId });
            }

            string grupoNormalizado = string.IsNullOrWhiteSpace(group) ? "Grupo A - Matutino" : group.Trim();

            // Evitar duplicar el mismo grupo/turno en el mismo ciclo para la misma materia
            bool yaExisteGrupo = await _context.DocenteAsignaturas.AnyAsync(da =>
                da.SubjectId == subjectId &&
                da.AcademicPeriodId == periodId &&
                da.Group == grupoNormalizado);

            if (yaExisteGrupo)
            {
                TempData["Error"] = $"El {grupoNormalizado} ya tiene un docente asignado para este ciclo escolar.";
                return RedirectToAction(nameof(Index), new { periodoId = periodId });
            }

            var nuevaAsignacion = new DocenteAsignatura
            {
                SubjectId = subjectId,
                AcademicPeriodId = periodId,
                DocenteId = docenteId,
                Group = grupoNormalizado
            };

            _context.DocenteAsignaturas.Add(nuevaAsignacion);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Docente asignado exitosamente al {grupoNormalizado}.";
            return RedirectToAction(nameof(Index), new { periodoId = periodId });
        }

        // POST: Subjects/UnassignTeacher
        // Desvincula un grupo/turno específico de una materia
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera,Administrador")]
        public async Task<IActionResult> UnassignTeacher(int assignmentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var asignacion = await _context.DocenteAsignaturas
                .Include(da => da.Subject)
                .FirstOrDefaultAsync(da => da.Id == assignmentId);

            if (asignacion == null) return NotFound();

            // 🔒 Validación de departamento
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                if (asignacion.Subject?.DepartamentoId != currentUser.DepartamentoId) return Forbid();
            }

            int periodId = asignacion.AcademicPeriodId ?? 0;
            _context.DocenteAsignaturas.Remove(asignacion);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Asignación de grupo desvinculada correctamente.";
            return RedirectToAction(nameof(Index), new { periodoId = periodId });
        }

        // GET: Subjects/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();

            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                if (subject.DepartamentoId != currentUser.DepartamentoId) return Forbid();
            }

            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                ViewBag.Departamentos = new SelectList(
                    await _context.Departamentos.Where(d => d.Id == currentUser.DepartamentoId).ToListAsync(),
                    "Id", "Name", subject.DepartamentoId);
            }
            else
            {
                ViewBag.Departamentos = new SelectList(
                    await _context.Departamentos.OrderBy(d => d.Name).ToListAsync(),
                    "Id", "Name", subject.DepartamentoId);
            }

            return View(subject);
        }

        // POST: Subjects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Subject subject)
        {
            if (id != subject.Id) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var materiaExistente = await _context.Subjects.FindAsync(id);
            if (materiaExistente == null) return NotFound();

            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                if (materiaExistente.DepartamentoId != currentUser.DepartamentoId) return Forbid();
                subject.DepartamentoId = currentUser.DepartamentoId.Value;
            }

            if (ModelState.IsValid)
            {
                materiaExistente.Code = subject.Code?.Trim().ToUpper() ?? string.Empty;
                materiaExistente.Name = subject.Name;
                materiaExistente.DepartamentoId = subject.DepartamentoId;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Datos de la asignatura actualizados correctamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Departamentos = new SelectList(
                await _context.Departamentos.OrderBy(d => d.Name).ToListAsync(),
                "Id", "Name", subject.DepartamentoId);

            return View(subject);
        }

        // POST: Subjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera,Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var subject = await _context.Subjects
                .Include(s => s.DocenteAsignaturas)
                .Include(s => s.Documents)
                .Include(s => s.AcademicTrackings)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            // 🔒 Seguridad Multi-Tenant: Un Jefe de Carrera no puede borrar materias de otra carrera
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                if (subject.DepartamentoId != currentUser?.DepartamentoId)
                {
                    TempData["Error"] = "No tienes autorización para eliminar asignaturas de otro departamento.";
                    return RedirectToAction(nameof(Index));
                }
            }

            try
            {
                // 1. Si tiene asignaciones de docentes/grupos, las retiramos primero
                if (subject.DocenteAsignaturas != null && subject.DocenteAsignaturas.Any())
                {
                    _context.DocenteAsignaturas.RemoveRange(subject.DocenteAsignaturas);
                }

                // 2. Eliminamos la materia del catálogo maestro
                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"La asignatura {subject.Name} fue eliminada del catálogo maestro.";
            }
            catch (DbUpdateException)
            {
                // 🛡️ Si ya tiene PDFs o calificaciones subidas en algún ciclo, SQL Server bloquea el borrado
                TempData["Error"] = "No es posible eliminar esta asignatura porque ya cuenta con evidencias documentales o calificaciones registradas en el historial. Primero deben removerse esos registros.";
            }

            return RedirectToAction(nameof(Index));
        }


        // Método auxiliar para obtener lista unificada de docentes
        private async Task<List<ApplicationUser>> ObtenerDocentesDisponibles()
        {
            var docentesPuros = await _userManager.GetUsersInRoleAsync("Docente");
            var jefesCarrera = await _userManager.GetUsersInRoleAsync("JefeCarrera");
            var jefesGenerales = await _userManager.GetUsersInRoleAsync("JefeGeneral");

            return docentesPuros
                .Union(jefesCarrera)
                .Union(jefesGenerales)
                .DistinctBy(u => u.Id)
                .OrderBy(u => u.FullName)
                .ToList();
        }

    }
}