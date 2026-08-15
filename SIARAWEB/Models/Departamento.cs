using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class Departamento
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del departamento es obligatorio.")]
        [StringLength(100)]
        [Display(Name = "Nombre del Departamento/Carrera")]
        public string Name { get; set; } = string.Empty;

        // 🟢 AQUÍ ESTÁ LA CORRECCIÓN: Cambiamos Clave por Code
        [StringLength(50)]
        [Display(Name = "Clave o Siglas")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Jefe de Carrera Asignado")]
        public string? HeadOfDepartmentId { get; set; }

        [ForeignKey("HeadOfDepartmentId")]
        public virtual ApplicationUser? HeadOfDepartment { get; set; }

        public virtual ICollection<Subject>? Subjects { get; set; }
    }
}