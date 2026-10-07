using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class DocumentTask
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El título de la tarea es obligatorio.")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Periodo al que pertenece la entrega
        [Required]
        public int AcademicPeriodId { get; set; }
        [ForeignKey("AcademicPeriodId")]
        public virtual AcademicPeriod? AcademicPeriod { get; set; }

        // Carrera / Departamento emisor
        [Required]
        public int DepartamentoId { get; set; }
        [ForeignKey("DepartamentoId")]
        public virtual Departamento? Departamento { get; set; }
        
        // Tipo de fase: "Inicial", "Seguimiento1", "Seguimiento2", "Final"
        [Required]
        [StringLength(30)]
        public string PhaseType { get; set; } = "Inicial";

        // Fecha y hora límite para marcar IsOnTime
        [Required]
        public DateTime DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Entregas asociadas
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual ICollection<AcademicTracking> AcademicTrackings { get; set; } = new List<AcademicTracking>();
    }
}