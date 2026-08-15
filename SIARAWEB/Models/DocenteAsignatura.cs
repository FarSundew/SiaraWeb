using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class DocenteAsignatura
    {
        [Key]
        public int Id { get; set; }

        // Relación con el Docente (Usuario)
        public string DocenteId { get; set; } = string.Empty;
        [ForeignKey("DocenteId")]
        public virtual ApplicationUser? Docente { get; set; }

        // Relación con la Asignatura
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }
    }
}