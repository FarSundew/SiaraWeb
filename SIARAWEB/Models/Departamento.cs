using System.ComponentModel.DataAnnotations;

namespace SIARAWEB.Models
{
    public class Departamento
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del departamento es obligatorio.")]
        [Display(Name = "Nombre del Departamento")]
        public string Nombre { get; set; }
    }
}