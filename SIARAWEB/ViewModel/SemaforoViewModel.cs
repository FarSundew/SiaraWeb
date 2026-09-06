namespace SIARAWEB.Models
{
    public class SemaforoViewModel
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string DocenteName { get; set; } = "Sin asignar";

        // Propiedades para los colores del semáforo (Verde, Rojo, Gris)
        public string ColorInicial { get; set; } = "bg-secondary";
        public string ColorSeg1 { get; set; } = "bg-secondary";
        public string ColorSeg2 { get; set; } = "bg-secondary";
        public string ColorFinal { get; set; } = "bg-secondary"; // Columna del Reporte Final
    }
}