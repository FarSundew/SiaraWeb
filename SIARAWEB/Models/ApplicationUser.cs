using Microsoft.AspNetCore.Identity;

namespace SIARAWEB.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? RFC { get; set; }
        public string? CURP { get; set; }
        public bool IsActive { get; set; } = true;

        // Relación con Departamento
        public int? DepartamentoId { get; set; }
        public Departamento? Departamento { get; set; }

        // 🟢 ESTA LÍNEA SOLUCIONA EL TERCER ERROR:
        public ICollection<DocenteAsignatura>? DocenteAsignaturas { get; set; }
    }
}