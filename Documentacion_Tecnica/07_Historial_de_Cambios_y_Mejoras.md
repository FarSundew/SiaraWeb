# 7. Historial Técnico de Cambios y Mejoras — SIARAWEB

A continuación se documentan las 23 mejoras arquitectónicas, funcionales y visuales implementadas en la plataforma:

---

## Bloque I: Módulos Core de Seguridad, Gestión Académica y Auditoría

1. **Candado jerárquico de roles y permisos (`DocentesController`, `ApplicationUsersController`, `Departamento`):**
   - Protección de cuentas raíz y vinculación del Jefe de Carrera titular (`HeadOfDepartmentId`) para impedir que un docente comisionado como Revisor Auxiliar modifique o revoque los permisos del titular.
2. **Bloqueo y entrega secuencial de seguimientos (`DocumentsController`, `AcademicTrackingsController`, `DocenteTasks.cshtml`):**
   - Validación en interfaz y servidor (`GET` y `POST`) que bloquea el **2do Seguimiento** si no se ha entregado el **1er Seguimiento**, y bloquea el **Reporte Final** si no se ha entregado el **2do Seguimiento**.
3. **Alta descentralizada de docentes (`DocentesController`, `Views/Docentes/Create.cshtml`):**
   - Registro directo de nuevos profesores desde el panel del Jefe de Carrera con adscripción automática a su departamento.
4. **Préstamo interdepartamental de docentes y aislamiento por carrera (`DocentesController`, `DocumentTasksController`, `ReportsController`):**
   - Asignación de materias a profesores de otros departamentos (*En Préstamo*) manteniendo la auditoría aislada exclusivamente en el departamento dueño de la asignatura.
5. **Asignación académica con Grupo y Turno (`DocenteAsignatura`, `Views/Docentes/Details.cshtml`):**
   - Incorporación de Grupo (`A`, `B`, `C`, `M`, `U`) y Turno (`Matutino`, `Vespertino`, `Mixto`) en la carga académica.
6. **Métrica de deserción en el panel analítico (`HomeController`, `ReportsController`):**
   - Integración del porcentaje de deserción (`DropoutPercentage`) junto con los índices de aprobación y reprobación.
7. **Segmentación de notificaciones (`Notification`, `NotificationsController`):**
   - Clasificación en pestañas de avisos **Importantes** (nuevos buzones, prórrogas y alertas) y **Normales** (resultados de revisión documental).
8. **Edición flexible de fechas de corte y prórrogas (`DocumentTasksController`, `Views/DocumentTasks/Edit.cshtml`):**
   - Ampliación de fecha y hora límite (`DueDate`) de los buzones sin afectar las evidencias ya entregadas.
9. **Selectores dinámicos de periodos escolares (`PeriodConfig`, `AcademicPeriodsController`):**
   - Generación y vinculación automática de ciclos escolares desde la base de datos y la configuración global.
10. **Dictamen de Acción Correctiva ante reprobación $\ge 40\%$ (`AcademicTracking`, `DocumentTasksController`, `Review.cshtml`):**
    - Obligatoriedad de registrar un Plan Remedial cuando una unidad tiene $\ge 40\%$ de reprobación y mesa de dictamen para aprobar o rechazar la estrategia.
11. **Exportación oficial a Excel y PDF (`ReportsController`, `PdfGeneratorService`):**
    - Descarga de reportes institucionales en formatos `.xlsx` (ClosedXML) y `.pdf` (QuestPDF).

---

## Bloque II: Optimizaciones de Flujo, Reportes Ejecutivos y Experiencia de Usuario

12. **Unificación de la asignación docente (`SubjectsController`, `Views/Subjects/Index.cshtml`):**
    - Centralización de la carga académica exclusivamente dentro del módulo de **Plantilla Docente**.
13. **Aislamiento estricto de buzones entre jefaturas (`DocumentTasksController`):**
    - Filtrado por `DepartamentoId` en todas las acciones de gestión y revisión para que cada carrera visualice únicamente sus propios seguimientos.
14. **Campo Semestre en Asignaturas y ordenamiento multicriterio (`Subject`, `SubjectsController`):**
    - Ubicación semestral (`1°` a `9°`) con filtros y ordenamiento por semestre, nombre, clave y créditos.
15. **Auto-asignación de materias para el Jefe de Carrera (`DocentesController`, `Views/Docentes/Index.cshtml`):**
    - Inclusión del Jefe de Carrera titular en su listado de plantilla docente para permitirle impartir cátedra.
16. **Depuración de controladores y vistas obsoletas:**
    - Eliminación de archivos heredados fuera de uso tras la migración al modelo unificado de `DocumentTasks`.
17. **Blindaje reforzado del titular en `ToggleReviewerRole` (`DocentesController`):**
    - Validación explícita para que únicamente el titular del departamento o el Administrador puedan gestionar comisiones de revisión auxiliar.
18. **Configuración global de etiquetas de periodos con selectores duales (`Periodo` + `Año`):**
    - Panel de configuración para el Administrador Técnico manteniendo los dos selectores independientes (`Semestre / Periodo` y `Año`) en los formularios de Jefatura de Carrera.
19. **Ampliación de paginación en el expediente del docente (`DocentesController`, `Views/Docentes/Details.cshtml`):**
    - Incremento de capacidad de `6` a **`15` asignaturas por página** en cuadrícula de 3 columnas para cubrir semestres completos en una sola vista.
20. **Identificación del departamento en los títulos de seguimientos (`DocumentTasksController`, `DocenteTasks.cshtml`):**
    - Visualización del formato `[Título del Seguimiento] - [Nombre del Departamento]` para orientar a los docentes que imparten clases en múltiples carreras.
21. **Persistencia de pestaña activa (`Semáforo` vs `Sábana`) y Visor Interactivo de PDF (`ReportsController`, `Views/Reports/Details.cshtml`):**
    - Retorno inteligente a la pestaña de origen en el tablero analítico y visor de PDF embebido con filtros rápidos por fase.
22. **Diferenciación de Reportes PDF y corrección del Reporte por Docente (`PdfGeneratorService`, `ReportsController`, `_SelectDocente.cshtml`, `_SelectDepartamento.cshtml`):**
    - Separación entre **Reporte Ejecutivo General** (4 secciones con KPIs, comparativo por departamento, matriz integral y bitácora de acciones correctivas $\ge 40\%$), **Semáforo de Entregas**, **Solo Sábana** y **Reporte Individual por Docente**.
    - Corrección del enlace de `docenteId` (`string` GUID) y de los modales de selección de docente y departamento.
23. **Reorganización del Lobby y Menú Lateral por rol + Codificación UTF-8 limpia (`Views/Home/Index.cshtml`, `Views/Shared/_Sidebar.cshtml`, `DocumentsController.cs`):**
    - Personalización de accesos rápidos según el perfil autenticado (eliminando enlaces no autorizados para el Subdirector y retirando el botón duplicado de notificaciones del menú lateral) y corrección total de acentos UTF-8 en código y base de datos.
