using System.Collections.Generic;
using SIARAWEB.Models;

namespace SIARAWEB.Models
{
    public class AnalyticsDashboardViewModel
    {
        // Filtros activos
        public int ActivePeriodId { get; set; }
        public int? SelectedDepartamentoId { get; set; }
        public string? SearchQuery { get; set; }

        // Catálogos para filtros
        public List<AcademicPeriod> AcademicPeriods { get; set; } = new();
        public List<Departamento> Departamentos { get; set; } = new();

        // KPIs Globales
        public int TotalAsignaturas { get; set; }
        public int DocentesEvaluados { get; set; }
        public decimal PromedioAprobacion { get; set; }
        public decimal PromedioReprobacion { get; set; }
        public int AlertasRiesgo { get; set; }

        // Datos para Semáforo
        public List<SemaforoRow> SemaforoRows { get; set; } = new();
        public int EntregasATiempo { get; set; }
        public int EntregasDesfasadas { get; set; }
        public int EntregasPendientes { get; set; }

        // Datos para Sábana
        public List<SabanaRow> SabanaRows { get; set; } = new();
    }

    public class SemaforoRow
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public string DocenteName { get; set; } = "";
        
        public string ColorInicial { get; set; } = "bg-secondary";
        public string ColorSeg1 { get; set; } = "bg-secondary";
        public string ColorSeg2 { get; set; } = "bg-secondary";
        public string ColorFinal { get; set; } = "bg-secondary";
    }

    public class SabanaRow
    {
        public int TrackingId { get; set; }
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public string TeacherName { get; set; } = "";
        public string PhaseName { get; set; } = "";
        public int UnitNumber { get; set; }
        public decimal ApprovalPercentage { get; set; }
        public decimal FailurePercentage { get; set; }
        public bool IsHighRisk => FailurePercentage >= 40.0m;
        public string ApprovalStatus { get; set; } = "";
        public string? CorrectiveAction { get; set; }
    }
}

