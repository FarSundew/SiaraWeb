using System.Collections.Generic;

namespace SIARAWEB.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public string Clave { get; set; } // Ej. H1B63 [1]
        public string Name { get; set; }
        public int Horas { get; set; } // Horas por semana [1]
        public int Temas { get; set; } // Cantidad de temas/unidades [1]
        public string Period { get; set; } // Semestre (Ej. 6TO SEMESTRE) [1]
        public int Year { get; set; }

        public List<DocenteAsignatura> DocenteAsignaturas { get; set; } = new List<DocenteAsignatura>();
        public List<AcademicTracking> AcademicTrackings { get; set; } = new List<AcademicTracking>();
        public List<Document> Documents { get; set; } = new List<Document>();
    }
}