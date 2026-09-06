using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class DocenteAsignatura
    {
        public int Id { get; set; }

        // Llave foránea hacia el Docente (ApplicationUser)
        [Required]
        public string DocenteId { get; set; } = string.Empty;

        [ForeignKey("DocenteId")]
        public ApplicationUser? Docente { get; set; }

        // Llave foránea hacia la Asignatura (Subject)
        [Required]
        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }
    }
}