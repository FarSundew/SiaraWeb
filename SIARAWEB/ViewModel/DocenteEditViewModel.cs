using System.ComponentModel.DataAnnotations;

namespace SIARAWEB.ViewModel
{
    public class DocenteEditViewModel
    {
        // Inicializamos con string.Empty para quitar la advertencia
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La CURP es obligatoria")]
        [StringLength(18, ErrorMessage = "La CURP debe tener 18 caracteres")]
        public string Curp { get; set; } = string.Empty;

        [Required(ErrorMessage = "El RFC es obligatorio")]
        [StringLength(13, ErrorMessage = "El RFC no puede exceder los 13 caracteres")]
        public string Rfc { get; set; } = string.Empty;
    }
}
