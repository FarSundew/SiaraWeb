# 4. Módulos, Controladores y Rutas — SIARAWEB

## 4.1 Mapa General de Controladores

### 1. `HomeController` (`/Home`)
- **Acceso:** Usuarios autenticados (`[Authorize]`).
- **Acciones:**
  - `GET /Home/Index`: Construye el **Lobby / Dashboard de Inicio** adaptado al rol del usuario autenticado.
    - Calcula para docentes su número de grupos asignados, documentos subidos y promedio de acreditación en el periodo activo.
    - Calcula para Jefes de Carrera las métricas filtradas exclusivamente por su `DepartamentoId`.
    - Calcula para Subdirección (`JefeGeneral`) y `Administrador` los indicadores globales de todas las carreras.

### 2. `DocentesController` (`/Docentes`)
- **Acceso:** `[Authorize(Roles = "JefeCarrera,Administrador")]`.
- **Acciones:**
  - `GET /Docentes/Index`: Lista la plantilla docente del departamento (incluyendo al propio Jefe de Carrera titular para permitir auto-asignación de materias y a los profesores en préstamo con carga activa en la carrera).
  - `GET / POST /Docentes/Create`: Alta descentralizada de nuevos profesores, asignándoles automáticamente el rol `Docente` y el `DepartamentoId` de la jefatura que los registra.
  - `GET /Docentes/Details/{id}`: Muestra el expediente de carga académica del docente con paginación de **15 asignaturas por página** (ordenadas por semestre y nombre) y formulario para asignar materias con **Periodo + Año**, **Grupo** y **Turno**.
  - `POST /Docentes/AsignarMateria`: Registra o actualiza una carga académica en `DocenteAsignaturas`, resolviendo o creando automáticamente el `AcademicPeriod` correspondiente a la combinación `Periodo + Año`.
  - `POST /Docentes/RemoverMateria`: Elimina una asignación de materia verificando que pertenezca al departamento del Jefe de Carrera.
  - `POST /Docentes/ToggleReviewerRole`: Otorga o revoca el rol de Revisor Auxiliar (`JefeCarrera`) a un docente del mismo departamento, bloqueando modificaciones sobre el Jefe de Carrera titular.

### 3. `SubjectsController` (`/Subjects`)
- **Acceso:** `[Authorize(Roles = "JefeCarrera,Administrador")]`.
- **Acciones:**
  - `GET /Subjects/Index`: Catálogo de asignaturas con filtrado por texto, filtro por semestre (`1°` al `9°`) y ordenamiento dinámico (`semestre`, `nombre`, `codigo`, `creditos`).
  - `GET / POST /Subjects/Create` y `Edit`: Registro y edición de asignaturas incluyendo clave, nombre, créditos, semestre y departamento.

### 4. `DocumentTasksController` (`/DocumentTasks`)
- **Acceso Mixto:**
  - `GET /DocumentTasks/DocenteTasks` (`[Authorize(Roles = "Docente,JefeCarrera")]`): Bandeja de tareas del docente agrupada por buzón, mostrando el nombre de la tarea junto con el nombre del departamento (`[Título] - [Departamento]`) y el estado de entrega de cada asignatura.
  - `GET /DocumentTasks/Index` (`[Authorize(Roles = "JefeCarrera,Administrador")]`): Panel de buzones emitidos por la jefatura.
  - `GET / POST /DocumentTasks/Create` y `Edit`: Creación de buzones por fase (`Inicial`, `Seguimiento1`, `Seguimiento2`, `Final`) mediante selectores dinámicos de `Periodo + Año` y edición de fechas límite (`DueDate`) para otorgar prórrogas, enviando notificaciones catalogadas como `Importante` a los docentes.
  - `GET /DocumentTasks/Review/{id}`: Mesa de dictamen donde la jefatura aprueba o rechaza documentos PDF y seguimientos de calificaciones/acciones correctivas.
  - `POST /DocumentTasks/ReviewDocument` y `ReviewTracking`: Registra el dictamen (`Aprobado` / `Rechazado`) y envía una notificación de tipo `Normal` al docente titular.

### 5. `DocumentsController` (`/Documents`)
- **Acceso:** `[Authorize(Roles = "Docente,JefeCarrera")]`.
- **Acciones:**
  - `GET /Documents/Upload/{id}?taskId={taskId}`: Muestra la lista de documentos requeridos según la fase del buzón (`Instrumentación Didáctica`, `Instrumentos de Evaluación`, `Prácticas de Laboratorio`, `Proyecto Individual`, `Evaluación Diagnóstica`, etc.) tras verificar el candado de entrega secuencial.
  - `POST /Documents/UploadTaskDocument`: Valida extensión `.pdf`, almacena el archivo en `wwwroot/uploads/documentos`, evalúa puntualidad inicial o registra fecha de corrección (`CorrectionSubmissionDate`).
  - `POST /Documents/SetNotApplicable`: Marca un documento específico como `No Aplica (N/A)` y lo aprueba automáticamente.
  - `POST /Documents/DeleteDocument`: Elimina el archivo físico y su registro en base de datos.

### 6. `AcademicTrackingsController` (`/AcademicTrackings`)
- **Acceso:** `[Authorize(Roles = "Docente,JefeCarrera")]`.
- **Acciones:**
  - `GET / POST /AcademicTrackings/Capture/{id}?taskId={taskId}`: Captura por unidad temática de los porcentajes de aprobación, reprobación y deserción, validando entrega secuencial y obligatoriedad del Plan Remedial cuando `FailurePercentage >= 40%`.

### 7. `ReportsController` (`/Reports`)
- **Acceso:** `[Authorize(Roles = "JefeCarrera,JefeGeneral,Administrador")]`.
- **Acciones:**
  - `GET /Reports/Index`: Tablero analítico con 3 pestañas sincronizadas (`tab=kpi`, `tab=semaforo`, `tab=sabana`) y filtros por periodo, departamento y búsqueda.
  - `GET /Reports/Details/{id}`: Expediente interactivo de asignatura con filtros por fase, visor de PDF embebido en vivo y persistencia de la pestaña de origen (`Semáforo` vs `Sábana`).
  - `GET /Reports/ExportPdf`: Genera los 4 reportes PDF mediante `PdfGeneratorService`.
  - `GET /Reports/ExportExcel`: Genera el archivo `.xlsx` mediante ClosedXML.
  - `GET /Reports/SelectDocente` y `SelectDepartamento`: Cargan los modales parciales para descarga filtrada por profesor o por carrera.
