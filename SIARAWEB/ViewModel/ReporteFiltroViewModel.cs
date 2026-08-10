using System.ComponentModel.DataAnnotations;

namespace SIARAWEB.ViewModels
{
    public class ReporteFiltroViewModel
    {
        [Display(Name = "DEPARTAMENTO")]
        public int? DepartamentoId { get; set; }

        [Display(Name = "SEMESTRE")]
        public string? Semestre { get; set; } // Ej. "ENERO - JUNIO"

        [Display(Name = "AÑO")]
        public int? Year { get; set; } = DateTime.Now.Year;

        [Display(Name = "TIPO DE SEGUIMIENTO")]
        public string? TipoSeguimiento { get; set; }

        [Display(Name = "ÁMBITO DEL REPORTE")]
        public string? AmbitoReporte { get; set; }

        [Display(Name = "TIPO DE GRÁFICA")]
        public string? TipoGrafica { get; set; } // "BARRAS" o "PASTEL"
    }
}