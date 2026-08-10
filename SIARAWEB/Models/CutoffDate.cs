using System.ComponentModel.DataAnnotations;

namespace SIARAWEB.Models
{
    public class CutoffDate
    {
        public int Id { get; set; }

        public int AcademicPeriodId { get; set; }
        public AcademicPeriod? AcademicPeriod { get; set; }

        [Display(Name = "Número de Seguimiento")]
        public int PhaseNumber { get; set; } // 1: 1er Seguimiento, 2: 2do, 3: 3er, 4: Reporte Final

        [Required]
        [Display(Name = "Nombre del Corte")]
        public string Name { get; set; } = string.Empty; // Ej. "Primer Seguimiento"

        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime DueDate { get; set; } // Fecha/Hora límite
    }
}