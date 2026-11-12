using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SIARAWEB.Models;

namespace SIARAWEB.Services
{
    public class PdfGeneratorService
    {
        // =========================================================================
        // 1. REPORTE EJECUTIVO GENERAL INTEGRAL (KPIs + Departamentos + Semáforo/Sábana + Acciones Correctivas)
        // =========================================================================
        public byte[] GenerateReporteGeneralPdf(
            List<Subject> subjects,
            List<AcademicTracking> allTrackings,
            AcademicPeriod? period,
            string userName)
        {
            decimal globalAprob = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.ApprovalPercentage), 1) : 0m;
            decimal globalReprob = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.FailurePercentage), 1) : 0m;
            decimal globalDeserc = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.DropoutPercentage), 1) : 0m;

            var allDocs = subjects.SelectMany(s => s.Documents ?? new List<SIARAWEB.Models.Document>()).ToList();
            int totalDocs = allDocs.Count;
            int docsATiempo = allDocs.Count(d => d.IsOnTime);
            int docsDesfasados = allDocs.Count(d => !d.IsOnTime && d.Status != "NoAplica");
            int docsAprobados = allDocs.Count(d => d.ApprovalStatus == "Aprobado");
            int docsRechazados = allDocs.Count(d => d.ApprovalStatus == "Rechazado");

            int totalDocentes = subjects
                .SelectMany(s => s.DocenteAsignaturas ?? new List<DocenteAsignatura>())
                .Where(da => !string.IsNullOrEmpty(da.DocenteId))
                .Select(da => da.DocenteId)
                .Distinct()
                .Count();

            var criticalTrackings = allTrackings
                .Where(t => t.FailurePercentage >= 40.0m)
                .OrderByDescending(t => t.FailurePercentage)
                .ToList();

            var deptoGroups = subjects
                .GroupBy(s => s.Departamento?.Name ?? "Sin Departamento")
                .OrderBy(g => g.Key)
                .ToList();

            string EvalFase(Subject s, string phase)
            {
                var docsFase = s.Documents?.Where(d => d.DocumentTask?.PhaseType == phase).ToList();
                var tracksFase = allTrackings.Where(t => t.SubjectId == s.Id && t.DocumentTask?.PhaseType == phase).ToList();
                bool hasD = docsFase != null && docsFase.Any();
                bool hasT = tracksFase.Any();
                if (!hasD && !hasT) return "PENDIENTE";
                return (docsFase != null && docsFase.Any(d => !d.IsOnTime)) ? "DESFASADO" : "A TIEMPO";
            }

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                    // ENCABEZADO INSTITUCIONAL
                    page.Header().Column(col =>
                    {
                        col.Item().Background("#112d60").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(headerCol =>
                            {
                                headerCol.Item()
                                    .Text("TECNOLÓGICO NACIONAL DE MÉXICO - INSTITUTO TECNOLÓGICO DE CAMPECHE")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor("#ffffff");
                                headerCol.Item()
                                    .Text($"REPORTE EJECUTIVO GENERAL DE AUDITORÍA Y RENDIMIENTO ACADÉMICO - {period?.Name.ToUpper() ?? "SIN PERIODO"}")
                                    .FontSize(9.5f)
                                    .Bold()
                                    .FontColor("#f4a915");
                            });
                            row.ConstantItem(210).AlignRight().Column(metaCol =>
                            {
                                metaCol.Item().AlignRight().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#e2e8f0");
                                metaCol.Item().AlignRight().Text($"Emitido por: {userName}").FontSize(8).Bold().FontColor("#ffffff");
                            });
                        });
                        col.Item().PaddingBottom(8);
                    });

                    // CONTENIDO INTEGRAL MULTISECCIÓN
                    page.Content().Column(col =>
                    {
                        // SECCIÓN 1: PANEL EJECUTIVO DE INDICADORES CLAVE (ACADÉMICOS + DOCUMENTALES)
                        col.Item().PaddingBottom(4).Text("1. RESUMEN EJECUTIVO DE INDICADORES INSTITUCIONALES (KPIs)").FontSize(9.5f).Bold().FontColor("#112d60");

                        col.Item().PaddingBottom(10).Row(kpiRow =>
                        {
                            kpiRow.RelativeItem().PaddingRight(3).Background("#f8fafc").Border(0.5f).BorderColor("#cbd5e1").Padding(6).Column(c =>
                            {
                                c.Item().Text("COBERTURA ACADÉMICA").FontSize(7).Bold().FontColor("#475569");
                                c.Item().Text($"{subjects.Count} Materias | {totalDocentes} Docentes").FontSize(10).Bold().FontColor("#112d60");
                                c.Item().Text($"{deptoGroups.Count} Carrera(s) evaluada(s)").FontSize(7.5f).FontColor("#64748b");
                            });

                            kpiRow.RelativeItem().PaddingHorizontal(3).Background("#f0fdf4").Border(0.5f).BorderColor("#bbf7d0").Padding(6).Column(c =>
                            {
                                c.Item().Text("ÍNDICE DE APROBACIÓN").FontSize(7).Bold().FontColor("#16a34a");
                                c.Item().Text($"{globalAprob:0.0}%").FontSize(12).Bold().FontColor("#16a34a");
                                c.Item().Text($"{allTrackings.Count} unidades registradas").FontSize(7.5f).FontColor("#15803d");
                            });

                            kpiRow.RelativeItem().PaddingHorizontal(3).Background("#fef2f2").Border(0.5f).BorderColor("#fecaca").Padding(6).Column(c =>
                            {
                                c.Item().Text("ÍNDICE DE REPROBACIÓN").FontSize(7).Bold().FontColor("#dc2626");
                                c.Item().Text($"{globalReprob:0.0}%").FontSize(12).Bold().FontColor("#dc2626");
                                c.Item().Text($"{criticalTrackings.Count} unidad(es) en riesgo (>=40%)").FontSize(7.5f).FontColor("#b91c1c");
                            });

                            kpiRow.RelativeItem().PaddingHorizontal(3).Background("#fffbeb").Border(0.5f).BorderColor("#fde68a").Padding(6).Column(c =>
                            {
                                c.Item().Text("ÍNDICE DE DESERCIÓN").FontSize(7).Bold().FontColor("#d97706");
                                c.Item().Text($"{globalDeserc:0.0}%").FontSize(12).Bold().FontColor("#d97706");
                                c.Item().Text("Promedio en el ciclo").FontSize(7.5f).FontColor("#b45309");
                            });

                            kpiRow.RelativeItem().PaddingLeft(3).Background("#eff6ff").Border(0.5f).BorderColor("#bfdbfe").Padding(6).Column(c =>
                            {
                                c.Item().Text("AUDITORÍA DOCUMENTAL").FontSize(7).Bold().FontColor("#1d4ed8");
                                c.Item().Text($"{totalDocs} Evidencias ({docsATiempo} a tiempo)").FontSize(9.5f).Bold().FontColor("#1e3a8a");
                                c.Item().Text($"{docsAprobados} aprobadas | {docsRechazados} rechazadas | {docsDesfasados} tardías").FontSize(7f).FontColor("#1d4ed8");
                            });
                        });

                        // SECCIÓN 2: RENDIMIENTO Y AUDITORÍA POR DEPARTAMENTO / CARRERA
                        col.Item().PaddingBottom(4).Text("2. CONCENTRADO COMPARATIVO POR DEPARTAMENTO / CARRERA").FontSize(9.5f).Bold().FontColor("#112d60");

                        col.Item().PaddingBottom(10).Table(deptoTable =>
                        {
                            deptoTable.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.6f); // Carrera
                                columns.ConstantColumn(60);   // Materias
                                columns.ConstantColumn(60);   // Docentes
                                columns.ConstantColumn(70);   // Evidencias
                                columns.ConstantColumn(75);   // Puntualidad
                                columns.ConstantColumn(65);   // % Aprob
                                columns.ConstantColumn(65);   // % Reprob
                                columns.ConstantColumn(65);   // % Deserc
                                columns.ConstantColumn(75);   // Alertas >=40%
                            });

                            deptoTable.Header(header =>
                            {
                                var hList = new[] { "DEPARTAMENTO / CARRERA", "MATERIAS", "DOCENTES", "EVIDENCIAS", "PUNTUALIDAD", "% APROB", "% REPROB", "% DESERC", "UNID. RIESGO" };
                                foreach (var h in hList)
                                {
                                    header.Cell().Background("#1e3a8a").Padding(4).AlignCenter().Text(h).FontColor("#ffffff").Bold().FontSize(7);
                                }
                            });

                            foreach (var grp in deptoGroups)
                            {
                                var grpSubjectIds = grp.Select(s => s.Id).ToList();
                                var grpTracks = allTrackings.Where(t => grpSubjectIds.Contains(t.SubjectId)).ToList();
                                var grpDocs = grp.SelectMany(s => s.Documents ?? new List<SIARAWEB.Models.Document>()).ToList();
                                int grpDocentes = grp.SelectMany(s => s.DocenteAsignaturas ?? new List<DocenteAsignatura>())
                                    .Select(da => da.DocenteId).Distinct().Count();

                                decimal dAprob = grpTracks.Any() ? Math.Round(grpTracks.Average(t => t.ApprovalPercentage), 1) : 0m;
                                decimal dReprob = grpTracks.Any() ? Math.Round(grpTracks.Average(t => t.FailurePercentage), 1) : 0m;
                                decimal dDeserc = grpTracks.Any() ? Math.Round(grpTracks.Average(t => t.DropoutPercentage), 1) : 0m;
                                int dRiesgo = grpTracks.Count(t => t.FailurePercentage >= 40.0m);
                                decimal pctPuntual = grpDocs.Any() ? Math.Round((decimal)grpDocs.Count(d => d.IsOnTime) * 100m / grpDocs.Count, 1) : 0m;

                                deptoTable.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4).Text(grp.Key).Bold().FontSize(7.5f);
                                deptoTable.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4).AlignCenter().Text(grp.Count().ToString()).FontSize(7.5f);
                                deptoTable.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4).AlignCenter().Text(grpDocentes.ToString()).FontSize(7.5f);
                                deptoTable.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4).AlignCenter().Text(grpDocs.Count.ToString()).FontSize(7.5f);
                                deptoTable.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4).AlignCenter().Text($"{pctPuntual:0.0}%").FontSize(7.5f);
                                deptoTable.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4).AlignCenter().Text($"{dAprob:0.0}%").FontColor("#16a34a").Bold().FontSize(7.5f);
                                deptoTable.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4).AlignCenter().Text($"{dReprob:0.0}%").FontColor(dReprob >= 40m ? "#dc2626" : "#1e293b").Bold().FontSize(7.5f);
                                deptoTable.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4).AlignCenter().Text($"{dDeserc:0.0}%").FontColor("#d97706").FontSize(7.5f);
                                deptoTable.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4).AlignCenter().Text(dRiesgo > 0 ? $"{dRiesgo} alerta(s)" : "0").FontColor(dRiesgo > 0 ? "#dc2626" : "#16a34a").Bold().FontSize(7.5f);
                            }
                        });

                        // SECCIÓN 3: MATRIZ INTEGRAL (SEMÁFORO DOCUMENTAL + SÁBANA DE RENDIMIENTO POR ASIGNATURA)
                        col.Item().PaddingBottom(4).Text("3. MATRIZ INTEGRAL DE SEGUIMIENTO DOCUMENTAL (SEMÁFORO) Y RENDIMIENTO ACADÉMICO").FontSize(9.5f).Bold().FontColor("#112d60");

                        col.Item().PaddingBottom(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(46);   // Código
                                columns.RelativeColumn(2.1f); // Asignatura
                                columns.RelativeColumn(1.5f); // Carrera
                                columns.RelativeColumn(1.7f); // Docente
                                columns.ConstantColumn(46);   // Inicial
                                columns.ConstantColumn(46);   // Seg 1
                                columns.ConstantColumn(46);   // Seg 2
                                columns.ConstantColumn(46);   // Final
                                columns.ConstantColumn(38);   // Docs
                                columns.ConstantColumn(44);   // % Aprob
                                columns.ConstantColumn(44);   // % Reprob
                                columns.ConstantColumn(44);   // % Deserc
                                columns.ConstantColumn(55);   // Riesgo
                            });

                            table.Header(header =>
                            {
                                var headers = new[] { "CLAVE", "ASIGNATURA", "CARRERA", "DOCENTE", "INICIAL", "SEG. 1", "SEG. 2", "FINAL", "DOCS", "% APR", "% REP", "% DES", "ESTATUS" };
                                foreach (var h in headers)
                                {
                                    header.Cell().Background("#112d60").Padding(3).AlignCenter().Text(h).FontColor("#ffffff").Bold().FontSize(6.5f);
                                }
                            });

                            foreach (var s in subjects)
                            {
                                var tracks = allTrackings.Where(t => t.SubjectId == s.Id).ToList();
                                decimal avgA = tracks.Any() ? Math.Round(tracks.Average(t => t.ApprovalPercentage), 1) : 0m;
                                decimal avgR = tracks.Any() ? Math.Round(tracks.Average(t => t.FailurePercentage), 1) : 0m;
                                decimal avgD = tracks.Any() ? Math.Round(tracks.Average(t => t.DropoutPercentage), 1) : 0m;
                                int riskCount = tracks.Count(t => t.FailurePercentage >= 40.0m);
                                bool isRisk = avgR >= 40.0m || riskCount > 0;
                                string rowBg = isRisk ? "#fee2e2" : "#ffffff";

                                string stIni = EvalFase(s, "Inicial");
                                string stS1 = EvalFase(s, "Seguimiento1");
                                string stS2 = EvalFase(s, "Seguimiento2");
                                string stFin = EvalFase(s, "Final");

                                string ColorFase(string st) => st == "A TIEMPO" ? "#16a34a" : (st == "DESFASADO" ? "#dc2626" : "#64748b");

                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text(s.Code).FontSize(6.5f);
                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text(s.Name).Bold().FontSize(6.5f);
                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text(s.Departamento?.Code ?? s.Departamento?.Name ?? "-").FontSize(6.5f);
                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text(s.DocenteAsignaturas?.FirstOrDefault()?.Docente?.FullName ?? "Sin Asignar").FontSize(6.5f);

                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignCenter().Text(stIni).FontColor(ColorFase(stIni)).Bold().FontSize(6f);
                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignCenter().Text(stS1).FontColor(ColorFase(stS1)).Bold().FontSize(6f);
                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignCenter().Text(stS2).FontColor(ColorFase(stS2)).Bold().FontSize(6f);
                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignCenter().Text(stFin).FontColor(ColorFase(stFin)).Bold().FontSize(6f);

                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignCenter().Text((s.Documents?.Count ?? 0).ToString()).FontSize(6.5f);
                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignCenter().Text($"{avgA:0.0}%").FontColor("#16a34a").Bold().FontSize(6.5f);
                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignCenter().Text($"{avgR:0.0}%").FontColor(isRisk ? "#dc2626" : "#1e293b").Bold().FontSize(6.5f);
                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignCenter().Text($"{avgD:0.0}%").FontColor("#d97706").FontSize(6.5f);
                                table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignCenter().Text(isRisk ? $"RIESGO ({riskCount})" : "NORMAL").FontColor(isRisk ? "#dc2626" : "#16a34a").Bold().FontSize(6.5f);
                            }
                        });

                        // SECCIÓN 4: DESGLOSE DE UNIDADES EN RIESGO CRÍTICO (>= 40% REPROBACIÓN) Y ACCIONES CORRECTIVAS
                        col.Item().PaddingBottom(4).Text("4. BITÁCORA DE ALERTAS CRÍTICAS (>= 40% REPROBACIÓN) Y DICTAMEN DE ACCIONES CORRECTIVAS").FontSize(9.5f).Bold().FontColor("#dc2626");

                        if (criticalTrackings.Any())
                        {
                            col.Item().Table(critTable =>
                            {
                                critTable.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.8f); // Materia
                                    columns.RelativeColumn(1.5f); // Docente
                                    columns.ConstantColumn(50);   // Unidad
                                    columns.ConstantColumn(55);   // % Reprob
                                    columns.RelativeColumn(2.6f); // Acción Correctiva / Plan Remedial
                                    columns.ConstantColumn(70);   // Dictamen Jefatura
                                });

                                critTable.Header(header =>
                                {
                                    var hList = new[] { "ASIGNATURA", "DOCENTE TITULAR", "UNIDAD", "% REPROB", "ESTRATEGIA REMEDIAL / ACCIÓN CORRECTIVA", "DICTAMEN" };
                                    foreach (var h in hList)
                                    {
                                        header.Cell().Background("#991b1b").Padding(4).AlignCenter().Text(h).FontColor("#ffffff").Bold().FontSize(7);
                                    }
                                });

                                foreach (var ct in criticalTrackings)
                                {
                                    var subj = subjects.FirstOrDefault(s => s.Id == ct.SubjectId);
                                    string docName = subj?.DocenteAsignaturas?.FirstOrDefault()?.Docente?.FullName ?? "Sin Asignar";
                                    string remedial = string.IsNullOrWhiteSpace(ct.Observations) ? "Sin estrategia remedial registrada" : ct.Observations;

                                    critTable.Cell().BorderBottom(0.5f).BorderColor("#fecaca").Background("#fff5f5").Padding(4).Text($"{subj?.Code} - {subj?.Name}").Bold().FontSize(7);
                                    critTable.Cell().BorderBottom(0.5f).BorderColor("#fecaca").Background("#fff5f5").Padding(4).Text(docName).FontSize(7);
                                    critTable.Cell().BorderBottom(0.5f).BorderColor("#fecaca").Background("#fff5f5").Padding(4).AlignCenter().Text($"Unidad {ct.UnitNumber}").Bold().FontSize(7);
                                    critTable.Cell().BorderBottom(0.5f).BorderColor("#fecaca").Background("#fff5f5").Padding(4).AlignCenter().Text($"{ct.FailurePercentage:0.0}%").FontColor("#dc2626").Bold().FontSize(7.5f);
                                    critTable.Cell().BorderBottom(0.5f).BorderColor("#fecaca").Background("#fff5f5").Padding(4).Text(remedial).FontSize(7);
                                    critTable.Cell().BorderBottom(0.5f).BorderColor("#fecaca").Background("#fff5f5").Padding(4).AlignCenter().Text(ct.ApprovalStatus.ToUpper()).Bold().FontSize(7);
                                }
                            });
                        }
                        else
                        {
                            col.Item().Background("#f0fdf4").Border(0.5f).BorderColor("#bbf7d0").Padding(8)
                                .Text("Excelente: No se registran unidades con índice de reprobación crítico (>= 40%) en este periodo.")
                                .FontColor("#16a34a").Bold().FontSize(8);
                        }
                    });

                    // PIE DE PÁGINA
                    page.Footer().AlignCenter().PaddingTop(8).BorderTop(0.5f).BorderColor("#cbd5e1").Text(x =>
                    {
                        x.DefaultTextStyle(s => s.FontSize(7.5f).FontColor("#64748b"));
                        x.Span("SIARA - Reporte Ejecutivo General de Auditoría Académica | Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        // =========================================================================
        // 2. REPORTE DE SEMÁFORO DE ENTREGAS DOCUMENTALES
        // =========================================================================
        public byte[] GenerateSemaforoPdf(
            List<Subject> subjects,
            List<AcademicTracking> allTrackings,
            AcademicPeriod? period,
            string userName)
        {
            var allDocs = subjects.SelectMany(s => s.Documents ?? new List<SIARAWEB.Models.Document>()).ToList();
            int docsATiempo = allDocs.Count(d => d.IsOnTime);
            int docsTardios = allDocs.Count(d => !d.IsOnTime && d.Status != "NoAplica");
            int docsAprobados = allDocs.Count(d => d.ApprovalStatus == "Aprobado");

            string EvalFase(Subject s, string phase)
            {
                var docsFase = s.Documents?.Where(d => d.DocumentTask?.PhaseType == phase).ToList();
                var tracksFase = allTrackings.Where(t => t.SubjectId == s.Id && t.DocumentTask?.PhaseType == phase).ToList();
                bool hasD = docsFase != null && docsFase.Any();
                bool hasT = tracksFase.Any();
                if (!hasD && !hasT) return "PENDIENTE";
                return (docsFase != null && docsFase.Any(d => !d.IsOnTime)) ? "DESFASADO" : "A TIEMPO";
            }

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(headerCol =>
                            {
                                headerCol.Item().Text("TECNOLÓGICO NACIONAL DE MÉXICO - INSTITUTO TECNOLÓGICO DE CAMPECHE").FontSize(13).Bold().FontColor("#112d60");
                                headerCol.Item().Text($"SEMÁFORO DE AUDITORÍA Y CUMPLIMIENTO DOCUMENTAL - {period?.Name.ToUpper() ?? "SIN PERIODO"}").FontSize(10.5f).Bold().FontColor("#374151");
                                headerCol.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm} | Emitido por: {userName}").FontSize(8).FontColor("#6b7280");
                            });
                        });

                        col.Item().PaddingVertical(6).LineHorizontal(1.5f).LineColor("#112d60");

                        col.Item().PaddingBottom(8).Row(kpiRow =>
                        {
                            kpiRow.RelativeItem().Column(c =>
                            {
                                c.Item().Text("ASIGNATURAS AUDITADAS").FontSize(7.5f).Bold().FontColor("#6b7280");
                                c.Item().Text($"{subjects.Count}").FontSize(12).Bold().FontColor("#112d60");
                            });
                            kpiRow.RelativeItem().Column(c =>
                            {
                                c.Item().Text("TOTAL EVIDENCIAS").FontSize(7.5f).Bold().FontColor("#1d4ed8");
                                c.Item().Text($"{allDocs.Count}").FontSize(12).Bold().FontColor("#1d4ed8");
                            });
                            kpiRow.RelativeItem().Column(c =>
                            {
                                c.Item().Text("ENTREGAS A TIEMPO").FontSize(7.5f).Bold().FontColor("#16a34a");
                                c.Item().Text($"{docsATiempo}").FontSize(12).Bold().FontColor("#16a34a");
                            });
                            kpiRow.RelativeItem().Column(c =>
                            {
                                c.Item().Text("ENTREGAS DESFASADAS").FontSize(7.5f).Bold().FontColor("#dc2626");
                                c.Item().Text($"{docsTardios}").FontSize(12).Bold().FontColor("#dc2626");
                            });
                            kpiRow.RelativeItem().Column(c =>
                            {
                                c.Item().Text("DOCUMENTOS VALIDADOS").FontSize(7.5f).Bold().FontColor("#0f766e");
                                c.Item().Text($"{docsAprobados}").FontSize(12).Bold().FontColor("#0f766e");
                            });
                        });
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(55);
                            columns.RelativeColumn(2.4f);
                            columns.RelativeColumn(1.8f);
                            columns.RelativeColumn(2.0f);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(60);
                        });

                        table.Header(header =>
                        {
                            var headers = new[] { "CÓDIGO", "ASIGNATURA", "DEPARTAMENTO", "DOCENTE TITULAR", "FASE INICIAL", "1ER SEGUIMIENTO", "2DO SEGUIMIENTO", "REPORTE FINAL", "TOTAL DOCS" };
                            foreach (var h in headers)
                            {
                                header.Cell().Background("#112d60").Padding(4).AlignCenter().Text(h).FontColor("#ffffff").Bold().FontSize(7);
                            }
                        });

                        foreach (var s in subjects)
                        {
                            string stIni = EvalFase(s, "Inicial");
                            string stS1 = EvalFase(s, "Seguimiento1");
                            string stS2 = EvalFase(s, "Seguimiento2");
                            string stFin = EvalFase(s, "Final");
                            string ColorFase(string st) => st == "A TIEMPO" ? "#16a34a" : (st == "DESFASADO" ? "#dc2626" : "#64748b");

                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).Text(s.Code).FontSize(7);
                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).Text(s.Name).Bold().FontSize(7);
                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).Text(s.Departamento?.Name ?? "General").FontSize(7);
                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).Text(s.DocenteAsignaturas?.FirstOrDefault()?.Docente?.FullName ?? "Sin Asignar").FontSize(7);

                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).AlignCenter().Text(stIni).FontColor(ColorFase(stIni)).Bold().FontSize(7);
                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).AlignCenter().Text(stS1).FontColor(ColorFase(stS1)).Bold().FontSize(7);
                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).AlignCenter().Text(stS2).FontColor(ColorFase(stS2)).Bold().FontSize(7);
                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).AlignCenter().Text(stFin).FontColor(ColorFase(stFin)).Bold().FontSize(7);
                            table.Cell().BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).AlignCenter().Text((s.Documents?.Count ?? 0).ToString()).Bold().FontSize(7);
                        }
                    });

                    page.Footer().AlignCenter().PaddingTop(8).BorderTop(0.5f).BorderColor("#e5e7eb").Text(x =>
                    {
                        x.DefaultTextStyle(s => s.FontSize(8).FontColor("#9ca3af"));
                        x.Span("SIARA - Semáforo de Entregas Documentales | Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        // =========================================================================
        // 3. SÁBANA DE RENDIMIENTO ACADÉMICO (SOLO SÁBANA)
        // =========================================================================
        public byte[] GenerateSabanaPdf(
            List<Subject> subjects,
            List<AcademicTracking> allTrackings,
            AcademicPeriod? period,
            string userName)
        {
            decimal globalAprob = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.ApprovalPercentage), 1) : 0m;
            decimal globalReprob = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.FailurePercentage), 1) : 0m;
            decimal globalDeserc = allTrackings.Any() ? Math.Round(allTrackings.Average(t => t.DropoutPercentage), 1) : 0m;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                    // HEADER MEJORADO
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(headerCol =>
                            {
                                headerCol.Item()
                                    .Text("TECNOLÓGICO NACIONAL DE MÉXICO - INSTITUTO TECNOLÓGICO DE CAMPECHE")
                                    .FontSize(13)
                                    .Bold()
                                    .FontColor("#112d60");
                                headerCol.Item()
                                    .Text($"SÁBANA DE RENDIMIENTO ACADÉMICO - {period?.Name.ToUpper() ?? "SIN PERIODO"}")
                                    .FontSize(11)
                                    .Bold()
                                    .FontColor("#374151");
                                headerCol.Item()
                                    .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm} | Emitido por: {userName}")
                                    .FontSize(8)
                                    .FontColor("#6b7280");
                            });
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1.5f).LineColor("#112d60");

                        // KPIs en fila
                        col.Item().PaddingBottom(10).Row(kpiRow =>
                        {
                            kpiRow.RelativeItem().Column(c =>
                            {
                                c.Item().Text("MATERIAS EVALUADAS").FontSize(8).Bold().FontColor("#6b7280");
                                c.Item().Text($"{subjects.Count}").FontSize(12).Bold().FontColor("#112d60");
                            });

                            kpiRow.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PROM. APROBACIÓN").FontSize(8).Bold().FontColor("#16a34a");
                                c.Item().Text($"{globalAprob}%").FontSize(12).Bold().FontColor("#16a34a");
                            });

                            kpiRow.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PROM. REPROBACIÓN").FontSize(8).Bold().FontColor("#dc2626");
                                c.Item().Text($"{globalReprob}%").FontSize(12).Bold().FontColor("#dc2626");
                            });

                            kpiRow.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PROM. DESERCIÓN").FontSize(8).Bold().FontColor("#d97706");
                                c.Item().Text($"{globalDeserc}%").FontSize(12).Bold().FontColor("#d97706");
                            });
                        });
                    });

                    // CONTENIDO - TABLA
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(55);   // Código
                            columns.RelativeColumn(2.8f); // Asignatura
                            columns.RelativeColumn(2.2f); // Docente
                            columns.ConstantColumn(45);   // Temas
                            columns.ConstantColumn(55);   // % Aprob
                            columns.ConstantColumn(55);   // % Reprob
                            columns.ConstantColumn(55);   // % Deserc
                            columns.ConstantColumn(65);   // Alertas
                        });

                        // ENCABEZADOS
                        table.Header(header =>
                        {
                            var headers = new[] { "CÓDIGO", "ASIGNATURA", "DOCENTE", "TEMAS", "% APROB", "% REPROB", "% DESERC", "RIESGO" };
                            foreach (var headerText in headers)
                            {
                                header.Cell()
                                    .Background("#112d60")
                                    .Padding(4)
                                    .AlignCenter()
                                    .Text(headerText)
                                    .FontColor("#ffffff")
                                    .Bold()
                                    .FontSize(7);
                            }
                        });

                        // FILAS DE DATOS
                        foreach (var subject in subjects)
                        {
                            var tracks = allTrackings.Where(t => t.SubjectId == subject.Id).ToList();
                            decimal avgA = tracks.Any() ? Math.Round(tracks.Average(t => t.ApprovalPercentage), 1) : 0m;
                            decimal avgR = tracks.Any() ? Math.Round(tracks.Average(t => t.FailurePercentage), 1) : 0m;
                            decimal avgD = tracks.Any() ? Math.Round(tracks.Average(t => t.DropoutPercentage), 1) : 0m;
                            int riskCount = tracks.Count(t => t.FailurePercentage >= 40.0m);
                            bool isRisk = avgR >= 40.0m || riskCount > 0;

                            string rowBg = isRisk ? "#fee2e2" : "#f9fafb";
                            string riskText = isRisk ? $"ALERTA: {riskCount}" : "OK";

                            // Código
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(3).Text(subject.Code).FontSize(7);

                            // Asignatura
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(3).Text(subject.Name).Bold().FontSize(7);

                            // Docente
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(3)
                                .Text(subject.DocenteAsignaturas?.FirstOrDefault()?.Docente?.FullName ?? "Sin Asignar").FontSize(7);

                            // Cantidad de temas
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(3).AlignCenter()
                                .Text(tracks.Count.ToString()).FontSize(7);

                            // % Aprobación
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(3).AlignCenter()
                                .Text($"{avgA:0.0}%").FontColor("#16a34a").Bold().FontSize(7);

                            // % Reprobación
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(3).AlignCenter()
                                .Text($"{avgR:0.0}%").FontColor(isRisk ? "#dc2626" : "#000000").Bold().FontSize(7);

                            // % Deserción
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(3).AlignCenter()
                                .Text($"{avgD:0.0}%").FontColor("#d97706").FontSize(7);

                            // Estado de Riesgo
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(3).AlignCenter()
                                .Text(riskText).FontColor(isRisk ? "#dc2626" : "#16a34a").Bold().FontSize(7);
                        }
                    });

                    // FOOTER
                    page.Footer().AlignCenter().PaddingTop(10).BorderTop(0.5f).BorderColor("#e5e7eb").Text(x =>
                    {
                        x.DefaultTextStyle(s => s.FontSize(8).FontColor("#9ca3af"));
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                        x.Span(" | SIARA - Sábana de Rendimiento Académico");
                    });
                });
            });

            return document.GeneratePdf();
        }

        // =========================================================================
        // 4. REPORTE INDIVIDUAL POR DOCENTE
        // =========================================================================
        public byte[] GeneratePdfByDocente(
            ApplicationUser docente,
            List<Subject> subjects,
            List<AcademicTracking> trackings,
            AcademicPeriod? period,
            string userName)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                    // HEADER
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(headerCol =>
                            {
                                headerCol.Item().Text("TECNOLÓGICO NACIONAL DE MÉXICO - INSTITUTO TECNOLÓGICO DE CAMPECHE").FontSize(12).Bold().FontColor("#112d60");
                                headerCol.Item().Text($"REPORTE INDIVIDUAL DE DESEMPEÑO DOCENTE - {period?.Name.ToUpper() ?? "SIN PERIODO"}").FontSize(10).Bold().FontColor("#374151");
                            });
                        });

                        col.Item().PaddingVertical(5).LineHorizontal(1.5f).LineColor("#112d60");

                        col.Item().PaddingVertical(6).Background("#f8fafc").Padding(8).Row(infoRow =>
                        {
                            infoRow.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("DOCENTE").FontSize(7).Bold().FontColor("#6b7280");
                                c.Item().Text(docente.FullName).FontSize(10).Bold().FontColor("#112d60");
                                c.Item().Text(docente.Email ?? "").FontSize(8).FontColor("#6b7280");
                            });

                            infoRow.RelativeItem(1.5f).Column(c =>
                            {
                                c.Item().Text("DEPARTAMENTO ADSCRITO").FontSize(7).Bold().FontColor("#6b7280");
                                c.Item().Text(docente.Departamento?.Name ?? "General").FontSize(9).Bold();
                            });

                            infoRow.RelativeItem(1).Column(c =>
                            {
                                c.Item().Text("MATERIAS EN PERIODO").FontSize(7).Bold().FontColor("#6b7280");
                                c.Item().Text($"{subjects.Count} asignatura(s)").FontSize(9).Bold();
                            });
                        });
                    });

                    // CONTENT
                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        decimal avgAprob = trackings.Any() ? Math.Round(trackings.Average(t => t.ApprovalPercentage), 1) : 0m;
                        decimal avgReprob = trackings.Any() ? Math.Round(trackings.Average(t => t.FailurePercentage), 1) : 0m;
                        decimal avgDeserc = trackings.Any() ? Math.Round(trackings.Average(t => t.DropoutPercentage), 1) : 0m;
                        int totalDocs = subjects.Sum(s => s.Documents?.Count ?? 0);

                        // KPIs Globales del Docente
                        col.Item().PaddingBottom(12).Row(kpiRow =>
                        {
                            kpiRow.RelativeItem().PaddingRight(4).Background("#f0fdf4").Padding(8).Column(kpi =>
                            {
                                kpi.Item().Text("PROM. APROBACIÓN").FontSize(8).Bold().FontColor("#16a34a");
                                kpi.Item().Text($"{avgAprob:0.0}%").FontSize(14).Bold().FontColor("#16a34a");
                            });

                            kpiRow.RelativeItem().PaddingHorizontal(4).Background("#fef2f2").Padding(8).Column(kpi =>
                            {
                                kpi.Item().Text("PROM. REPROBACIÓN").FontSize(8).Bold().FontColor("#dc2626");
                                kpi.Item().Text($"{avgReprob:0.0}%").FontSize(14).Bold().FontColor("#dc2626");
                            });

                            kpiRow.RelativeItem().PaddingHorizontal(4).Background("#fffbeb").Padding(8).Column(kpi =>
                            {
                                kpi.Item().Text("PROM. DESERCIÓN").FontSize(8).Bold().FontColor("#d97706");
                                kpi.Item().Text($"{avgDeserc:0.0}%").FontSize(14).Bold().FontColor("#d97706");
                            });

                            kpiRow.RelativeItem().PaddingLeft(4).Background("#eff6ff").Padding(8).Column(kpi =>
                            {
                                kpi.Item().Text("EVIDENCIAS SUBIDAS").FontSize(8).Bold().FontColor("#1d4ed8");
                                kpi.Item().Text($"{totalDocs} doc(s)").FontSize(14).Bold().FontColor("#1d4ed8");
                            });
                        });

                        // Detalle por cada Asignatura del Docente
                        foreach (var subject in subjects)
                        {
                            var subTrackings = trackings.Where(t => t.SubjectId == subject.Id).OrderBy(t => t.UnitNumber).ToList();
                            var subDocs = subject.Documents?.OrderBy(d => d.DocumentTaskId).ToList() ?? new List<SIARAWEB.Models.Document>();
                            var asigInfo = subject.DocenteAsignaturas?.FirstOrDefault(da => da.DocenteId == docente.Id);

                            col.Item().PaddingTop(8).PaddingBottom(4).Background("#112d60").Padding(6).Row(r =>
                            {
                                r.RelativeItem().Text($"{subject.Code} - {subject.Name} ({(subject.Departamento?.Name ?? "General")})")
                                    .FontColor("#ffffff").Bold().FontSize(9);
                                r.ConstantItem(150).AlignRight().Text($"{asigInfo?.Group ?? "Grupo A"} | Docs: {subDocs.Count}")
                                    .FontColor("#f4a915").Bold().FontSize(8);
                            });

                            if (subTrackings.Any())
                            {
                                col.Item().PaddingBottom(8).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(55);
                                        columns.RelativeColumn(1.6f);
                                        columns.ConstantColumn(60);
                                        columns.ConstantColumn(60);
                                        columns.ConstantColumn(60);
                                        columns.RelativeColumn(1.4f);
                                    });

                                    table.Header(header =>
                                    {
                                        var headers = new[] { "UNIDAD", "FASE / CORTE", "% APROB", "% REPROB", "% DESERC", "DICTAMEN / OBSERVACIONES" };
                                        foreach (var h in headers)
                                        {
                                            header.Cell().Background("#e2e8f0").Padding(4).Text(h).FontColor("#1e293b").Bold().FontSize(7.5f);
                                        }
                                    });

                                    foreach (var tr in subTrackings)
                                    {
                                        bool isHighFailure = tr.FailurePercentage >= 40m;
                                        table.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4)
                                            .Text($"Unidad {tr.UnitNumber}").Bold().FontSize(8);

                                        table.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4)
                                            .Text(tr.DocumentTask?.Title ?? "Seguimiento").FontSize(8);

                                        table.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4).AlignCenter()
                                            .Text($"{tr.ApprovalPercentage:0.0}%").FontColor("#16a34a").Bold().FontSize(8);

                                        table.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4).AlignCenter()
                                            .Text($"{tr.FailurePercentage:0.0}%").FontColor(isHighFailure ? "#dc2626" : "#1e293b").Bold().FontSize(8);

                                        table.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4).AlignCenter()
                                            .Text($"{tr.DropoutPercentage:0.0}%").FontColor("#d97706").FontSize(8);

                                        string obsText = string.IsNullOrWhiteSpace(tr.Observations)
                                            ? tr.ApprovalStatus
                                            : $"{tr.ApprovalStatus}: {tr.Observations}";
                                        table.Cell().BorderBottom(0.5f).BorderColor("#cbd5e1").Padding(4)
                                            .Text(obsText).FontSize(7.5f);
                                    }
                                });
                            }
                            else
                            {
                                col.Item().PaddingBottom(8).Background("#f8fafc").Border(0.5f).BorderColor("#e2e8f0").Padding(6)
                                    .Text("Sin calificaciones por unidad registradas en este periodo para esta asignatura.")
                                    .Italic().FontSize(8).FontColor("#64748b");
                            }
                        }
                    });

                    // FOOTER
                    page.Footer().AlignCenter().PaddingTop(10).BorderTop(0.5f).BorderColor("#e5e7eb").Text(x =>
                    {
                        x.DefaultTextStyle(s => s.FontSize(8).FontColor("#9ca3af"));
                        x.Span($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm} | Emitido por: {userName} | Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
