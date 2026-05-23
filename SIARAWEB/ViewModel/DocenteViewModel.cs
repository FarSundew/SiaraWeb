using System.ComponentModel.DataAnnotations;

namespace SIARAWEB.ViewModels
{
    public class DocenteViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "La CURP es obligatoria")]
        [StringLength(18, ErrorMessage = "La CURP debe tener 18 caracteres")]
        public string? Curp { get; set; }

        [Required(ErrorMessage = "El RFC es obligatorio")]
        [StringLength(13, ErrorMessage = "El RFC no puede exceder los 13 caracteres")]
        public string? Rfc { get; set; }

        [Required(ErrorMessage = "La contraseña temporal es obligatoria")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}