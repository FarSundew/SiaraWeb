namespace SIARAWEB.Models
{
    public class DocenteAsignatura
    {
        // Claves Foráneas de la relación M:N
        public string DocenteId { get; set; } = string.Empty;
        public ApplicationUser? Docente { get; set; }

        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }
    }
}