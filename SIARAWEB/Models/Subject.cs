using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class Subject
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        // Relación con Departamento
        public int DepartamentoId { get; set; }
        public Departamento? Departamento { get; set; }

        [NotMapped]
        public int? AcademicPeriodId { get; set; }
        
        [NotMapped]
        public AcademicPeriod? AcademicPeriod { get; set; }

        public ICollection<DocenteAsignatura>? DocenteAsignaturas { get; set; }

        public ICollection<AcademicTracking>? AcademicTrackings { get; set; }
        public ICollection<Document>? Documents { get; set; }
    }
}
