using System.ComponentModel.DataAnnotations;

namespace SIARAWEB.Models
{
    public class AcademicPeriod
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nombre del Periodo")]
        public string Name { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<CutoffDate>? CutoffDates { get; set; }

        // 🟢 ESTA ES LA LÍNEA QUE FALTA PARA RESOLVER EL ERROR:
        public ICollection<Subject>? Subjects { get; set; }
    }
}