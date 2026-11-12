using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        public int? DocumentTaskId { get; set; }
        [ForeignKey("DocumentTaskId")]
        public virtual DocumentTask? DocumentTask { get; set; }

        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        public int? CutoffDateId { get; set; }
        [ForeignKey("CutoffDateId")]
        public virtual CutoffDate? CutoffDate { get; set; }

        [Required]
        [StringLength(150)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public DateTime? CorrectionSubmissionDate { get; set; }

        public bool IsOnTime { get; set; } = true;
        public string Status { get; set; } = "EnTiempo";

        public string ApprovalStatus { get; set; } = "Pendiente";
        public string? Feedback { get; set; }
    }
}