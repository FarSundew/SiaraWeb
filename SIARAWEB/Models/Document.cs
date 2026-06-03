using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        // Llave foránea hacia la Asignatura
        [Required(ErrorMessage = "La asignatura es obligatoria")]
        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; } // El "?" soluciona la advertencia de NULL

        // Tipo de Documento (Instrumentación, Práctica, etc.)
        [Required(ErrorMessage = "El tipo de documento es obligatorio")]
        public string Type { get; set; } = string.Empty; // "= string.Empty" soluciona la advertencia de NULL

        // Ruta física donde se guardará el PDF
        public string? FilePath { get; set; } // El "?" permite que esté nulo antes de subirlo

        // Fecha exacta de la subida
        public DateTime UploadedAt { get; set; }

        // ¿Se entregó antes de la fecha de corte configurada por el Administrador?
        public bool IsOnTime { get; set; }
    }
}