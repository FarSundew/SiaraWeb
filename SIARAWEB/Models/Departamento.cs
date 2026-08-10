using System.ComponentModel.DataAnnotations;

namespace SIARAWEB.Models
{
    public class Departamento
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nombre del Departamento")]
        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        // Propiedad de navegación
        public ICollection<ApplicationUser>? Users { get; set; }
        public ICollection<Subject>? Subjects { get; set; }
    }
}