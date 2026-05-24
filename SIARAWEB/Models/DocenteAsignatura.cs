namespace SIARAWEB.Models
{
    public class DocenteAsignatura
    {
        public string DocenteId { get; set; }
        public int SubjectId { get; set; }

        public ApplicationUser Docente { get; set; }
        public Subject Subject { get; set; }
    }
}