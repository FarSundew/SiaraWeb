using System.Collections.Generic;
using SIARAWEB.Models;

namespace SIARAWEB.ViewModels
{
    public class FinalReportConsolidatedViewModel
    {
        public Subject Subject { get; set; } = null!;
        public CutoffDate? CutoffDateFinal { get; set; }
        public bool IsReadOnly { get; set; }

        // Unidades evaluadas durante el semestre
        public List<AcademicTracking> UnitTrackings { get; set; } = new();

        // Promedios acumulados de las unidades
        public decimal UnitsAverageApproval { get; set; }
        public decimal UnitsAverageFailure { get; set; }
        public decimal UnitsAverageDropout { get; set; }

        // Documentos entregados en la fase final (Acta, Cierre de Proyecto, etc.)
        public List<Document> FinalDocuments { get; set; } = new();

        // Datos del Cierre Definitivo
        public FinalSubjectGrade FinalGrade { get; set; } = new();
    }
}