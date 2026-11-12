import sys
import re

content = open('Controllers/DocumentsController.cs', 'r', encoding='cp1252').read()

new_method = '''public async Task<IActionResult> Upload(int id, int taskId)
        {
            var subject = await _context.Subjects
                .Include(s => s.Documents)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            var task = await _context.DocumentTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return NotFound("La tarea/buzón no existe.");

            var documentosRequeridos = new List<string>();
            switch (task.PhaseType)
            {
                case "Inicial":
                    documentosRequeridos = new List<string> {
                        "Instrumentación Didáctica",
                        "Instrumentos de Evaluación",
                        "Prácticas de Laboratorio",
                        "Proyecto Individual",
                        "Evaluación Diagnóstica"
                    };
                    break;
                case "Seguimiento1":
                    documentosRequeridos = new List<string> {
                        "Avance (apart. 6)",
                        "Calif. Parc. (Calificaciones Parciales)",
                        "Instr. Eval. (Instrumentos de Evaluación)",
                        "Eval. Diagn. (Evaluación Diagnóstica)",
                        "Avance Proy. Ind. (Proyecto Individual)"
                    };
                    break;
                case "Seguimiento2":
                    documentosRequeridos = new List<string> {
                        "Avance Programático (apart. 6)",
                        "Instrumentos de Evaluación",
                        "Reporte de Seguimiento Intermedio"
                    };
                    break;
                case "Final":
                    documentosRequeridos = new List<string> {
                        "Acta de Calificaciones",
                        "Instrumentos de Evaluación Finales",
                        "Cierre de Proyecto / Reporte Final"
                    };
                    break;
            }

            ViewBag.Task = task;
            ViewBag.DocumentosRequeridos = documentosRequeridos;

            return View(subject);
        }'''

# Replace the method Upload
new_content = re.sub(r'public async Task<IActionResult> Upload\(int id, string\? faseSeleccionada\).*?return View\(subject\);\s*\}', new_method, content, flags=re.DOTALL)

# Replace the RedirectToActions
new_content = re.sub(r'RedirectToAction\(nameof\(Upload\), new \{ id = subjectId, faseSeleccionada = task\.PhaseType \}\)', 'RedirectToAction(nameof(Upload), new { id = subjectId, taskId = documentTaskId })', new_content)
new_content = re.sub(r'RedirectToAction\(nameof\(Upload\), new \{ id = subjectId \}\)', 'RedirectToAction(nameof(Upload), new { id = subjectId, taskId = documentTaskId })', new_content)
new_content = re.sub(r'RedirectToAction\(nameof\(Upload\), new \{ id = subjectId, faseSeleccionada = faseRetorno \}\)', 'RedirectToAction(nameof(Upload), new { id = subjectId, taskId = doc?.DocumentTaskId ?? 0 })', new_content)
new_content = re.sub(r'RedirectToAction\(nameof\(Upload\), new \{ id = subjectId, faseSeleccionada = task\?\.PhaseType \}\)', 'RedirectToAction(nameof(Upload), new { id = subjectId, taskId = documentTaskId })', new_content)

with open('Controllers/DocumentsController.cs', 'w', encoding='cp1252') as f:
    f.write(new_content)
print('Updated Upload method and RedirectToActions')
