using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class DocenteAsignatura
    {
        [Required]
        public string DocenteId { get; set; } = string.Empty;
        [ForeignKey("DocenteId")]
        public virtual ApplicationUser? Docente { get; set; }

        [Required]
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        public int? AcademicPeriodId { get; set; }
        [ForeignKey("AcademicPeriodId")]
        public virtual AcademicPeriod? AcademicPeriod { get; set; }

        public string Group { get; set; } = "Grupo A";
    }
}