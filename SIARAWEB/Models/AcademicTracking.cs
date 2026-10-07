using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class AcademicTracking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Relación con el buzón de la tarea
        public int? DocumentTaskId { get; set; }
        [ForeignKey("DocumentTaskId")]
        public virtual DocumentTask? DocumentTask { get; set; }

        // Relación con la asignatura
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        public int? CutoffDateId { get; set; }
        [ForeignKey("CutoffDateId")]
        public virtual CutoffDate? CutoffDate { get; set; }

        [Required]
        [Display(Name = "Unidad / Tema")]
        public int UnitNumber { get; set; }

        public int TotalStudents { get; set; }
        public int ApprovedStudents { get; set; }
        public int FailedStudents { get; set; }
        public int DroppedStudents { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal ApprovalPercentage { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal FailurePercentage { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DropoutPercentage { get; set; }

        public string? Observations { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string ApprovalStatus { get; set; } = "Pendiente";
        public string? Feedback { get; set; }
    }
}