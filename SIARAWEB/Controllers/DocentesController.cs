using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    // 🔒 Acceso para el Jefe de Carrera, Jefe General y Soporte Técnico
    [Authorize(Roles = "JefeCarrera,Administrador")]
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

            // 1. Identificar a todos los usuarios con rol "Docente" y "JefeCarrera" (para permitir auto-asignación académica)
            var docentesList = await _userManager.GetUsersInRoleAsync("Docente");
            var jefesList = await _userManager.GetUsersInRoleAsync("JefeCarrera");
            var docentesIds = docentesList.Select(d => d.Id)
                                         .Union(jefesList.Select(j => j.Id))
                                         .Distinct()
                                         .ToList();

            // 2. Consulta base incluyendo departamento y asignaciones
            var query = _context.Users
                .Where(u => docentesIds.Contains(u.Id))
                .Include(u => u.Departamento)
                .Include(u => u.DocenteAsignaturas!)
                    .ThenInclude(da => da.Subject)
                        
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

            int miDeptoId = currentUser.DepartamentoId ?? 0;
            ViewBag.MiDepartamentoId = miDeptoId;

            // Identificar quién es el Jefe de Carrera Titular del departamento actual
            var miDepto = await _context.Departamentos.FirstOrDefaultAsync(d => d.Id == miDeptoId);
            ViewBag.JefeTitularId = miDepto?.HeadOfDepartmentId;

            // Para el préstamo entre carreras: Lista de todos los docentes del instituto
            var todosDocentes = await _context.Users
                .Where(u => docentesIds.Contains(u.Id))
                .Include(u => u.Departamento)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            ViewBag.TodosDocentes = todosDocentes;

            // Asignaturas de la carrera del usuario para asignación directa
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                ViewBag.MisAsignaturas = await _context.Subjects
                    .Where(s => s.DepartamentoId == currentUser.DepartamentoId)
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }
            else
            {
                ViewBag.MisAsignaturas = await _context.Subjects
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }

            ViewBag.AcademicPeriods = await _context.AcademicPeriods.OrderByDescending(p => p.Id).ToListAsync();

            return View(docentesConMaterias);
        }

        // GET: Docentes/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // Si es Jefe de Carrera, se restringe a su departamento; si es Administrador, todos los departamentos
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                ViewBag.Departamentos = await _context.Departamentos
                    .Where(d => d.Id == currentUser.DepartamentoId)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
                ViewBag.DepartamentoPredeterminadoId = currentUser.DepartamentoId;
            }
            else
            {
                ViewBag.Departamentos = await _context.Departamentos
                    .OrderBy(d => d.Name)
                    .ToListAsync();
                ViewBag.DepartamentoPredeterminadoId = null;
            }

            return View();
        }

        // POST: Docentes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser docente, string password)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // Si es Jefe de Carrera, se fuerza la adscripción a su departamento
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                docente.DepartamentoId = currentUser.DepartamentoId;
            }

            docente.UserName = docente.Email;
            docente.EmailConfirmed = true;
            docente.IsActive = true;

            ModelState.Remove("password");
            ModelState.Remove("DocenteAsignaturas");
            ModelState.Remove("Departamento");

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Password", "La contraseña temporal es requerida.");
            }

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrWhiteSpace(docente.RFC))
                    docente.RFC = docente.RFC.Trim().ToUpper();

                if (!string.IsNullOrWhiteSpace(docente.CURP))
                    docente.CURP = docente.CURP.Trim().ToUpper();

                var result = await _userManager.CreateAsync(docente, password);
                if (result.Succeeded)
                {
                    // Asignar automáticamente el rol "Docente"
                    await _userManager.AddToRoleAsync(docente, "Docente");
                    TempData["Success"] = $"El docente {docente.FullName} ha sido dado de alta exitosamente en la academia.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Recargar departamentos si falla
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                ViewBag.Departamentos = await _context.Departamentos
                    .Where(d => d.Id == currentUser.DepartamentoId)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
                ViewBag.DepartamentoPredeterminadoId = currentUser.DepartamentoId;
            }
            else
            {
                ViewBag.Departamentos = await _context.Departamentos
                    .OrderBy(d => d.Name)
                    .ToListAsync();
                ViewBag.DepartamentoPredeterminadoId = docente.DepartamentoId;
            }

            return View(docente);
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
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                ViewBag.Subjects = await _context.Subjects.Where(s => s.DepartamentoId == (user != null ? user.DepartamentoId : null)).ToListAsync();
            }
            else
            {
                ViewBag.Subjects = await _context.Subjects.ToListAsync();
            }
            ViewBag.AcademicPeriods = await _context.AcademicPeriods.OrderByDescending(p => p.Id).ToListAsync();

            // Suficiente capacidad por página para abarcar la carga completa de al menos un semestre (hasta 15 materias por página)
            int pageSize = 15;

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
                .ThenBy(da => da.Subject != null ? da.Subject.Semestre : 99)
                .ThenBy(da => da.Subject != null ? da.Subject.Name : "")
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
        [Authorize(Roles = "JefeCarrera,Administrador")]
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

            // 🔒 REGLA DE SEGURIDAD ESTRICTA: Candado jerárquico de roles y permisos
            // 1. No se puede modificar a uno mismo
            if (currentUser.Id == docente.Id)
            {
                TempData["Error"] = "Operación denegada: No puedes modificar tus propios permisos jerárquicos.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Verificar si el usuario objetivo es Jefe Titular de algún departamento
            bool objetivoEsJefeTitular = await _context.Departamentos.AnyAsync(d => d.HeadOfDepartmentId == docente.Id);
            if (objetivoEsJefeTitular)
            {
                TempData["Error"] = "Operación denegada: No se pueden revocar ni alterar las facultades del Jefe de Carrera Titular.";
                return RedirectToAction(nameof(Index));
            }

            // 3. Verificar que quien ejecuta la acción sea el Jefe de Carrera Titular (o Administrador), NO un docente comisionado auxiliar
            bool ejecutorEsAdmin = await _userManager.IsInRoleAsync(currentUser, "Administrador");
            var deptoActual = await _context.Departamentos.FirstOrDefaultAsync(d => d.Id == currentUser.DepartamentoId);
            bool ejecutorEsJefeTitular = deptoActual != null && deptoActual.HeadOfDepartmentId == currentUser.Id;

            // Si el departamento aún no tenía HeadOfDepartmentId asignado pero el usuario actual NO es un docente comisionado (o es el único titular), lo protegemos
            if (!ejecutorEsAdmin && !ejecutorEsJefeTitular)
            {
                TempData["Error"] = "Operación denegada: Solo el Jefe de Carrera Titular puede otorgar o revocar comisiones de revisión auxiliar.";
                return RedirectToAction(nameof(Index));
            }

            // 4. No se puede revocar ni alterar facultades de un Administrador general o Jefe General
            if (await _userManager.IsInRoleAsync(docente, "Administrador") || await _userManager.IsInRoleAsync(docente, "JefeGeneral"))
            {
                TempData["Error"] = "Operación denegada: No es posible alterar los privilegios de un Administrador o Jefe General.";
                return RedirectToAction(nameof(Index));
            }

            // 5. Solo se puede comisionar a docentes ADSCRITOS a la carrera del Jefe autenticado
            if (!ejecutorEsAdmin && docente.DepartamentoId != currentUser.DepartamentoId)
            {
                TempData["Error"] = "Operación denegada: No puedes otorgar permisos de revisor auxiliar a un docente adscrito a otra carrera.";
                return RedirectToAction(nameof(Index));
            }

            bool isReviewer = await _userManager.IsInRoleAsync(docente, "JefeCarrera");

            if (isReviewer)
            {
                await _userManager.RemoveFromRoleAsync(docente, "JefeCarrera");
                // Garantizar que siempre conserve su rol base de Docente para no dejar la cuenta huérfana
                if (!await _userManager.IsInRoleAsync(docente, "Docente"))
                {
                    await _userManager.AddToRoleAsync(docente, "Docente");
                }
                TempData["Success"] = $"Se retiraron los permisos de revisión auxiliar al docente {docente.FullName}.";
            }
            else
            {
                if (!await _userManager.IsInRoleAsync(docente, "Docente"))
                {
                    await _userManager.AddToRoleAsync(docente, "Docente");
                }
                await _userManager.AddToRoleAsync(docente, "JefeCarrera");
                TempData["Success"] = $"El docente {docente.FullName} fue comisionado como revisor auxiliar de la academia.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Docentes/AssignTeacher
        // Permite al Jefe de Carrera asignar una materia de su departamento a un docente (propio o en préstamo)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera,Administrador")]
        public async Task<IActionResult> AssignTeacher(int subjectId, int? academicPeriodId, string? semestre, int? anio, string docenteId, string group, string turno)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var subject = await _context.Subjects.FindAsync(subjectId);
            if (subject == null) return NotFound();

            // 🔒 Validación: El Jefe de Carrera solo puede asignar asignaturas pertenecientes a su carrera
            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                if (subject.DepartamentoId != currentUser.DepartamentoId)
                {
                    TempData["Error"] = "No tienes autorización para asignar asignaturas de otro departamento.";
                    return RedirectToAction("Details", new { id = docenteId });
                }
            }

            if (string.IsNullOrWhiteSpace(docenteId))
            {
                TempData["Error"] = "Debes seleccionar un docente titular.";
                return RedirectToAction("Details", new { id = docenteId });
            }

            var docente = await _context.Users.Include(u => u.Departamento).FirstOrDefaultAsync(u => u.Id == docenteId);
            if (docente == null)
            {
                TempData["Error"] = "El docente seleccionado no existe.";
                return RedirectToAction(nameof(Index));
            }

            AcademicPeriod? period = null;
            if (!string.IsNullOrWhiteSpace(semestre) && anio.HasValue)
            {
                string periodName = $"{semestre.Trim()} {anio.Value}".Trim();
                period = await _context.AcademicPeriods
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == periodName.ToLower());

                if (period == null)
                {
                    bool esEneroJunio = periodName.Contains("Enero", StringComparison.OrdinalIgnoreCase)
                                     || periodName.Contains("Primer", StringComparison.OrdinalIgnoreCase);
                    int y = anio.Value;
                    period = new AcademicPeriod
                    {
                        Name = periodName,
                        StartDate = esEneroJunio ? new DateTime(y, 1, 1) : new DateTime(y, 7, 1),
                        EndDate = esEneroJunio ? new DateTime(y, 6, 30) : new DateTime(y, 12, 31),
                        IsActive = true
                    };
                    _context.AcademicPeriods.Add(period);
                    await _context.SaveChangesAsync();
                }
            }
            else if (academicPeriodId.HasValue && academicPeriodId.Value > 0)
            {
                period = await _context.AcademicPeriods.FindAsync(academicPeriodId.Value);
            }

            if (period == null)
            {
                period = await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsActive)
                         ?? await _context.AcademicPeriods.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
            }

            if (period == null)
            {
                TempData["Error"] = "No se encontró un periodo académico válido.";
                return RedirectToAction("Details", new { id = docenteId });
            }

            string grupoNormalizado = string.IsNullOrWhiteSpace(group) ? "Grupo A" : group.Trim();
            string turnoNormalizado = string.IsNullOrWhiteSpace(turno) ? "Matutino" : turno.Trim();

            // Evitar duplicar el mismo grupo/turno en el mismo ciclo para la misma materia
            bool yaExisteGrupo = await _context.DocenteAsignaturas.AnyAsync(da =>
                da.SubjectId == subjectId &&
                da.AcademicPeriodId == period.Id &&
                da.Group == grupoNormalizado &&
                da.Turno == turnoNormalizado);

            if (yaExisteGrupo)
            {
                TempData["Error"] = $"El {grupoNormalizado} ({turnoNormalizado}) ya tiene un docente asignado para {period.Name}.";
                return RedirectToAction("Details", new { id = docenteId });
            }

            var asignacionExistente = await _context.DocenteAsignaturas
                .FirstOrDefaultAsync(da => da.DocenteId == docenteId && da.SubjectId == subjectId);

            if (asignacionExistente != null)
            {
                asignacionExistente.AcademicPeriodId = period.Id;
                asignacionExistente.Group = grupoNormalizado;
                asignacionExistente.Turno = turnoNormalizado;
                _context.DocenteAsignaturas.Update(asignacionExistente);
            }
            else
            {
                var nuevaAsignacion = new DocenteAsignatura
                {
                    SubjectId = subjectId,
                    AcademicPeriodId = period.Id,
                    DocenteId = docenteId,
                    Group = grupoNormalizado,
                    Turno = turnoNormalizado
                };
                _context.DocenteAsignaturas.Add(nuevaAsignacion);
            }

            await _context.SaveChangesAsync();

            bool esPrestamo = docente.DepartamentoId != subject.DepartamentoId;
            if (esPrestamo)
            {
                TempData["Success"] = $"Materia asignada exitosamente en modalidad de PRÉSTAMO INTERDEPARTAMENTAL (Adscripción: {docente.Departamento?.Name ?? "Externo"}).";
            }
            else
            {
                TempData["Success"] = $"Docente asignado exitosamente al {grupoNormalizado} en {period.Name}.";
            }

            return RedirectToAction("Details", new { id = docenteId });
        }

        // POST: Docentes/UnassignTeacher
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JefeCarrera,Administrador")]
        public async Task<IActionResult> UnassignTeacher(string docenteId, int subjectId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var asignacion = await _context.DocenteAsignaturas
                .Include(da => da.Subject)
                .FirstOrDefaultAsync(da => da.DocenteId == docenteId && da.SubjectId == subjectId);

            if (asignacion == null) return NotFound();

            if (User.IsInRole("JefeCarrera") && !User.IsInRole("JefeGeneral") && !User.IsInRole("Administrador"))
            {
                if (asignacion.Subject?.DepartamentoId != currentUser.DepartamentoId) return Forbid();
            }

            _context.DocenteAsignaturas.Remove(asignacion);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Asignación de grupo desvinculada correctamente.";
            return RedirectToAction("Details", new { id = docenteId });
        }
    }
}

