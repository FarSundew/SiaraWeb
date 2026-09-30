using System;
using System.Collections.Generic;
using SIARAWEB.Models;

namespace SIARAWEB.Models
{
    public class GeneralTrackingReportViewModel
    {
        // Filtros de búsqueda
        public int? SelectedPeriodId { get; set; }
        public int? SelectedCutoffDateId { get; set; }
        public string? SearchTeacherOrSubject { get; set; }
        public bool OnlyHighRisk { get; set; } // Reprobación >= 40%

        // Catálogos para los selects
        public List<AcademicPeriod> AcademicPeriods { get; set; } = new();
        public List<CutoffDate> CutoffDates { get; set; } = new();

        // Resumen cuantitativo superior (KPIs)
        public int TotalGroupsEvaluated { get; set; }
        public int TotalHighRiskUnits { get; set; }
        public decimal GlobalApprovalAverage { get; set; }
        public decimal GlobalFailureAverage { get; set; }

        // Filas consolidadas
        public List<TrackingReportRowItem> TrackingRows { get; set; } = new();
    }

    public class TrackingReportRowItem
    {
        public int TrackingId { get; set; }
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string CareerName { get; set; } = string.Empty;

        public string PhaseName { get; set; } = string.Empty;
        public int UnitNumber { get; set; }

        public int TotalStudents { get; set; }
        public int ApprovedStudents { get; set; }
        public int FailedStudents { get; set; }
        public int DroppedStudents { get; set; }

        public decimal ApprovalPercentage { get; set; }
        public decimal FailurePercentage { get; set; }
        public decimal DropoutPercentage { get; set; }

        // Indicador de riesgo institucional
        public bool IsHighRisk => FailurePercentage >= 40.0m;
        public string? CorrectiveAction { get; set; }
        public string ApprovalStatus { get; set; } = "Pendiente";
    }

    // Alias para compatibilidad con código existente que busque GeneralReportViewModel
    public class GeneralReportViewModel : GeneralTrackingReportViewModel
    {
    }
}