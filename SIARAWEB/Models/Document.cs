using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class Document
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        public int CutoffDateId { get; set; }
        [ForeignKey("CutoffDateId")]
        public virtual CutoffDate? CutoffDate { get; set; }

        [Required]
        [StringLength(150)]
        public string DocumentType { get; set; } = string.Empty; // Instrumentación, Actas, Prácticas, etc.

        [Required]
        public string FilePath { get; set; } = string.Empty;

        // 🟢 Se CONSERVA: Fecha original de primera entrega (determina IsOnTime)
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // 🟢 NUEVO CAMPO: Solo se llena cuando el docente sube una corrección
        public DateTime? CorrectionSubmissionDate { get; set; }

        public bool IsOnTime { get; set; } // Calculado: UploadedAt <= CutoffDate.DueDate
        public string Status { get; set; } = "EnTiempo"; // "EnTiempo", "Atrasado", "N/A"

        // Sistema de revisión por Jefatura
        public string ApprovalStatus { get; set; } = "Pendiente"; // "Pendiente", "Aprobado", "Rechazado"
        public string? Feedback { get; set; }
    }
}