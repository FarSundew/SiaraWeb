using System.ComponentModel.DataAnnotations;

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

        // Relación con Periodo Escolar
        public int AcademicPeriodId { get; set; }
        public AcademicPeriod? AcademicPeriod { get; set; }

        // 🟢 ESTA LÍNEA SOLUCIONA EL SEGUNDO ERROR:
        public ICollection<DocenteAsignatura>? DocenteAsignaturas { get; set; }

        public ICollection<AcademicTracking>? AcademicTrackings { get; set; }
        public ICollection<Document>? Documents { get; set; }
    }
}