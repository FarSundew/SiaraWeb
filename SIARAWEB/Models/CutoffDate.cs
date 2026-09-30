using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class CutoffDate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del corte es obligatorio.")]
        [StringLength(150)]
        [Display(Name = "Nombre de la Fecha de Corte")]
        public string Name { get; set; } = string.Empty; // Ej. "Primer Seguimiento"

        [Required]
        [Display(Name = "Fecha de Inicio")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "Fecha Límite")]
        public DateTime DueDate { get; set; }

        [Required]
        [Display(Name = "Tipo de Fase")]
        public string PhaseType { get; set; } = "Inicial"; // "Inicial", "Seguimiento1", "Seguimiento2", "Final"

        // Relación con Periodo Académico
        [Required]
        public int AcademicPeriodId { get; set; }
        [ForeignKey("AcademicPeriodId")]
        public virtual AcademicPeriod? AcademicPeriod { get; set; }

        // 🟢 FASE 4: Llave foránea hacia Departamento para fechas flexibles por academia
        [Display(Name = "Departamento / Carrera")]
        public int? DepartamentoId { get; set; }
        [ForeignKey("DepartamentoId")]
        public virtual Departamento? Departamento { get; set; }
    }
}