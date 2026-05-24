using System;

namespace SIARAWEB.Models
{
    public class Document
    {
        public int Id { get; set; }
        public int SubjectId { get; set; } // Llave foránea hacia la materia

        public string Type { get; set; } // Ej. "Instrumentación Didáctica", "Acta de Calificaciones"
        public string FilePath { get; set; } // La ruta donde se guardará el PDF
        public DateTime UploadedAt { get; set; } = DateTime.Now; // Fecha y hora de subida
        public bool IsOnTime { get; set; } // Valida si se entregó antes de la fecha límite

        // Propiedad de navegación
        public Subject Subject { get; set; }
    }
}
