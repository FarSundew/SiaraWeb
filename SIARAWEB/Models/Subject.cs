using System.Collections.Generic;

namespace SIARAWEB.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Period { get; set; }
        public int Year { get; set; }

        public List<DocenteAsignatura> DocenteAsignaturas { get; set; } = new List<DocenteAsignatura>();

        // ¡Nuevas relaciones de Uno a Muchos agregadas!
        public List<AcademicTracking> AcademicTrackings { get; set; } = new List<AcademicTracking>();
        public List<Document> Documents { get; set; } = new List<Document>();
    }
}