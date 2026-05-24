namespace SIARAWEB.Models
{
    public class AcademicTracking
    {
        public int Id { get; set; }
        public int SubjectId { get; set; } // Llave foránea hacia la materia

        public int UnitNumber { get; set; } // Del 1 al 6
        public double ApprovalPercentage { get; set; } // % de acreditación
        public double FailurePercentage { get; set; } // % de reprobación
        public double DropoutPercentage { get; set; } // % de deserción
        public string Phase { get; set; } // "1er Seguimiento", "2do Seguimiento", o "Reporte Final"

        // Propiedad de navegación
        public Subject Subject { get; set; }
    }
}