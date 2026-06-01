using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class DocenteAsignatura
    {
        // Llave foránea hacia el Docente
        public string DocenteId { get; set; }
        [ForeignKey("DocenteId")]
        public ApplicationUser Docente { get; set; }

        // Llave foránea hacia la Asignatura (Cambiado a SubjectId)
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public Subject Subject { get; set; }
    }
}