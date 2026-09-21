using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class FinalSubjectGrade
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        [Required]
        public int CutoffDateId { get; set; }
        [ForeignKey("CutoffDateId")]
        public virtual CutoffDate? CutoffDate { get; set; }

        [Display(Name = "Total de Estudiantes Inscritos")]
        public int TotalStudents { get; set; }

        [Display(Name = "Aprobados en Ordinario")]
        public int ApprovedRegularStudents { get; set; }

        [Display(Name = "Aprobados en Regularización / 2da Oportunidad")]
        public int ApprovedMakeupStudents { get; set; }

        [Display(Name = "Acreditados Finales (Total)")]
        public int FinalApprovedStudents { get; set; }

        [Display(Name = "Reprobados Finales")]
        public int FinalFailedStudents { get; set; }

        [Display(Name = "Desertores")]
        public int FinalDroppedStudents { get; set; }

        // Índices Calculados (%)
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "% Acreditación Final")]
        public decimal FinalApprovalPercentage { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "% Reprobación Final")]
        public decimal FinalFailurePercentage { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "% Deserción Final")]
        public decimal FinalDropoutPercentage { get; set; }

        [Display(Name = "Observaciones Generales de Cierre")]
        public string? Observations { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        public string ApprovalStatus { get; set; } = "Pendiente"; // "Pendiente", "Aprobado", "Rechazado"
    }
}