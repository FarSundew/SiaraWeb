namespace SIARAWEB.Models
{
    public class Document
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public int CutoffDateId { get; set; }
        public CutoffDate? CutoffDate { get; set; }

        public string DocumentType { get; set; } = string.Empty; // Instrumentación, Actas, etc.
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public bool IsOnTime { get; set; } // Calculado: UploadedAt <= CutoffDate.DueDate
        public string Status { get; set; } = "EnTiempo"; // "EnTiempo", "Atrasado"
    }
}