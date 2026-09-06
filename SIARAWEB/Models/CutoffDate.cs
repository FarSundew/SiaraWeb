using System.ComponentModel.DataAnnotations;

namespace SIARAWEB.Models
{
    public class CutoffDate
    {
        public int Id { get; set; }

        public int AcademicPeriodId { get; set; }
        public AcademicPeriod? AcademicPeriod { get; set; }

        [Required(ErrorMessage = "Debes seleccionar la etapa del semestre.")]
        [Display(Name = "Clasificación de la Fase")]
        public string PhaseType { get; set; } = string.Empty; // Opciones: "Inicial", "Seguimiento1", "Seguimiento2", "Final"

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [Display(Name = "Nombre Descriptivo del Corte")]
        public string Name { get; set; } = string.Empty; // Ej. "Entrega de Documentos Iniciales" o "Corte 1er Parcial"

        [Display(Name = "Fecha de Inicio")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }

        [Display(Name = "Fecha Límite (Cierre)")]
        [DataType(DataType.DateTime)]
        public DateTime DueDate { get; set; }
    }
}