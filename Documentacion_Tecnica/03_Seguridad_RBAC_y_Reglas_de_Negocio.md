# 3. Seguridad RBAC y Reglas de Negocio — SIARAWEB

## 3.1 Matriz de Control de Acceso Basado en Roles (RBAC)

El sistema implementa cuatro roles institucionales con separación estricta de privilegios en controladores y vistas:

| Módulo / Acción | `Administrador` (TI) | `JefeGeneral` (Subdirección) | `JefeCarrera` (Titular / Revisor) | `Docente` |
| :--- | :---: | :---: | :---: | :---: |
| Gestión de Usuarios y Roles (`ApplicationUsers`) | ✅ | ❌ | ❌ | ❌ |
| Gestión de Departamentos y Titulares (`Departamentos`) | ✅ | ❌ | ❌ | ❌ |
| Configuración Global de Periodos (`AcademicPeriods`) | ✅ | ❌ | ❌ | ❌ |
| Alta Descentralizada de Docentes (`Docentes/Create`) | ✅ | ❌ | ✅ (Su depto.) | ❌ |
| Asignación de Carga (Grupo, Turno, Préstamo) | ✅ | ❌ | ✅ (Sus materias) | ❌ |
| Otorgar/Revocar Comisión de Revisor Auxiliar | ✅ | ❌ | ✅ (Solo Titular) | ❌ |
| Catálogo de Asignaturas y Semestres (`Subjects`) | ✅ | ❌ | ✅ (Su depto.) | ❌ |
| Creación de Buzones y Prórrogas (`DocumentTasks`) | ✅ | ❌ | ✅ (Su depto.) | ❌ |
| Mesa de Dictamen Documental y Acción Correctiva | ✅ | ❌ | ✅ (Su depto.) | ❌ |
| Carga de Evidencias PDF y Calificaciones por Unidad | ❌ | ❌ | ✅ (Si imparte clase) | ✅ |
| Tablero Analítico (KPIs, Semáforo, Sábana, Visor PDF) | ✅ (Global) | ✅ (Global) | ✅ (Su depto.) | ❌ |
| Exportación de Reportes PDF y Excel | ✅ (Global) | ✅ (Global) | ✅ (Su depto.) | ❌ |

---

## 3.2 Reglas de Negocio Programadas

### RN-01: Candado Jerárquico e Inmunidad de Cuentas Raíz y Titulares
1. **Protección de Cuentas Raíz (`ApplicationUsersController`):** Las cuentas de administración general y subdirección están blindadas contra eliminación o degradación accidental de privilegios.
2. **Protección del Jefe de Carrera Titular (`DocentesController.ToggleReviewerRole`):**
   - Cada departamento registra a su titular oficial en `Departamento.HeadOfDepartmentId`.
   - Un Jefe de Carrera titular puede otorgar temporalmente el rol `JefeCarrera` a un docente adscrito a su mismo departamento para que actúe como **Revisor Auxiliar (Comisionado)**.
   - El sistema bloquea cualquier intento de un docente comisionado de revocar los permisos del Jefe de Carrera titular, modificar comisiones de otros docentes o auto-revocarse dejando sin titular a la academia.

### RN-02: Préstamo Interdepartamental con Aislamiento de Auditoría
- Un Jefe de Carrera puede asignar cualquiera de las asignaturas de su plan de estudios a un profesor adscrito a otro departamento académico (*Docente en Préstamo*).
- **Regla de aislamiento:** La auditoría de los documentos y calificaciones se rige por el `DepartamentoId` de la **asignatura** (`Subject.DepartamentoId`) y del **buzón** (`DocumentTask.DepartamentoId`), no por el departamento base del docente.
- Para evitar confusiones cuando un docente imparte materias en múltiples carreras, cada buzón y tarjeta de tarea muestra explícitamente la división académica: `[Título del Buzón] - [Nombre del Departamento]`.

### RN-03: Bloqueo de Entrega Secuencial por Fases
- El flujo documental del semestre se divide en cuatro fases cronológicas:
  1. `Inicial` (Instrumentación Didáctica, Instrumentos de Evaluación, Prácticas de Laboratorio, Proyecto Individual, Evaluación Diagnóstica).
  2. `Seguimiento1` (1er Seguimiento de avance programático y calificaciones parciales).
  3. `Seguimiento2` (2do Seguimiento intermedio).
  4. `Final` (Acta de calificaciones, instrumentos finales y reporte de cierre).
- **Candado secuencial (`DocumentsController` y `AcademicTrackingsController`):**
  - El sistema bloquea en la vista (`DocenteTasks.cshtml`) y en las acciones `GET`/`POST` del servidor cualquier subida o captura en `Seguimiento2` si la materia no cuenta con evidencias entregadas en `Seguimiento1` dentro del mismo periodo escolar.
  - De igual forma, bloquea la fase `Final` si la materia no ha cumplido con `Seguimiento2`.

### RN-04: Trazabilidad de Puntualidad y Reenvíos sin Penalización
- Al subir un archivo por primera vez, el sistema compara `DateTime.Now <= DocumentTask.DueDate`:
  - Si se entrega antes o en la fecha límite: `IsOnTime = true`, `Status = "EnTiempo"`.
  - Si se entrega después de la fecha límite: `IsOnTime = false`, `Status = "Atrasado"`.
- Si el Jefe de Carrera **rechaza** el documento con observaciones (`Feedback`) y el docente sube una versión corregida posteriormente:
  - El archivo PDF anterior se reemplaza en disco.
  - Se actualiza `CorrectionSubmissionDate = DateTime.Now` y el estado vuelve a `ApprovalStatus = "Pendiente"`.
  - **No se altera `UploadedAt` ni `IsOnTime`**, conservando el mérito de puntualidad de la primera entrega.

### RN-05: Validador de Acción Correctiva Obligatoria ante Reprobación $\ge 40\%$
- En `AcademicTrackingsController.Capture`:
  - La suma de `% Aprobación + % Reprobación` debe ser exactamente `100%` (con tolerancia decimal de `0.1`).
  - Si `FailurePercentage >= 40.0m` y el campo `Observations` está vacío, el servidor rechaza el registro y exige describir el **Plan Remedial / Acción Correctiva** que aplicará el docente.
- En la Mesa de Revisión (`DocumentTasksController.ReviewTracking`), las unidades con reprobación $\ge 40\%$ se destacan con alerta roja para que el Jefe de Carrera dictamine (`Aprobado` o `Rechazado`) tanto el seguimiento como la estrategia remedial propuesta.
