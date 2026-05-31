using System;

namespace SIARAWEB.Models
{
    public class Document
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }

        // Los 4 archivos obligatorios que pide el PDF
        public string? InstrumentacionPath { get; set; }
        public string? EvaluacionPath { get; set; }
        public string? PracticaPath { get; set; }
        public string? ProyectoPath { get; set; }

        public string? Observaciones { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public bool IsOnTime { get; set; }

        public Subject Subject { get; set; }
    }
}