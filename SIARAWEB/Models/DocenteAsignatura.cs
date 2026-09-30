using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class DocenteAsignatura
    {
        public int Id { get; set; }

        [Required]
        public string DocenteId { get; set; } = string.Empty;
        [ForeignKey("DocenteId")]
        public virtual ApplicationUser? Docente { get; set; }

        [Required]
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        // Periodo al que corresponde la impartición
        public int? AcademicPeriodId { get; set; }
        [ForeignKey("AcademicPeriodId")]
        public virtual AcademicPeriod? AcademicPeriod { get; set; }

        // 🟢 Identificador de Grupo / Turno
        [StringLength(20)]
        public string Group { get; set; } = "A"; // Ejemplos: "Matutino", "Vespertino", "A", "B"
    }
}