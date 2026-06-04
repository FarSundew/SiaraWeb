using System.ComponentModel.DataAnnotations;

namespace SIARAWEB.ViewModels
{
    public class DocenteEditViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "La CURP es obligatoria")]
        [StringLength(18, MinimumLength = 18, ErrorMessage = "La CURP debe tener exactamente 18 caracteres")]
        public string? Curp { get; set; }

        [Required(ErrorMessage = "El RFC es obligatorio")]
        [StringLength(13, MinimumLength = 12, ErrorMessage = "El RFC debe tener entre 12 y 13 caracteres")]
        public string? Rfc { get; set; }

        [Required(ErrorMessage = "El departamento es obligatorio")]
        public int? DepartamentoId { get; set; }
    }
}