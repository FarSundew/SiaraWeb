using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema; // ⚠️ Necesario para la llave foránea

namespace SIARAWEB.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }
        public string? Rfc { get; set; }
        public string? Curp { get; set; }
        public int? DepartamentoId { get; set; }

        // Navegación hacia Departamento
        [ForeignKey("DepartamentoId")]
        public Departamento? Departamento { get; set; }

        // Colección de navegación
        public ICollection<DocenteAsignatura> DocenteAsignaturas { get; set; } = new List<DocenteAsignatura>();
    }
}