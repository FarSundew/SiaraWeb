using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class AcademicTracking
    {
        public int Id { get; set; }

        // Relación con Asignatura
        [Required]
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        // Relación con Fecha de Corte / Fase (1er Seg, 2do Seg, etc.)
        [Required]
        public int CutoffDateId { get; set; }
        public CutoffDate? CutoffDate { get; set; }

        [Required]
        [Display(Name = "Número de Unidad / Tema")]
        public int UnitNumber { get; set; } // Tema 1, Tema 2, Tema 3...

        [Display(Name = "Total de Alumnos")]
        public int TotalStudents { get; set; }

        [Display(Name = "Aprobados")]
        public int ApprovedStudents { get; set; }

        [Display(Name = "Reprobados")]
        public int FailedStudents { get; set; }

        [Display(Name = "Desertores")]
        public int DroppedStudents { get; set; }

        // Porcentajes calculados
        [Display(Name = "% Aprobación")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal ApprovalPercentage { get; set; }

        [Display(Name = "% Reprobación")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal FailurePercentage { get; set; }

        [Display(Name = "% Deserción")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DropoutPercentage { get; set; }

        public string? Observations { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        // Agrega estas propiedades a tu modelo AcademicTracking
        public string ApprovalStatus { get; set; } = "Pendiente";
        public string? Feedback { get; set; }
    }
}