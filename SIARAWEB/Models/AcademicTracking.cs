namespace SIARAWEB.Models
{
    public class AcademicTracking
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public int CutoffDateId { get; set; }
        public CutoffDate? CutoffDate { get; set; }

        public int UnitNumber { get; set; } // Tema/Unidad
        public decimal ApprovalPercentage { get; set; }
        public decimal FailurePercentage { get; set; }
        public decimal DropoutPercentage { get; set; }
        public string? Observations { get; set; }
    }
}