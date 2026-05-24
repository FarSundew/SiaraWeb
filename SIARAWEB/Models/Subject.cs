using System.Collections.Generic;

namespace SIARAWEB.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Period { get; set; }
        public int Year { get; set; }

        // Propiedad de navegación hacia la tabla intermedia
        public List<DocenteAsignatura> DocenteAsignaturas { get; set; } = new List<DocenteAsignatura>();
    }
}