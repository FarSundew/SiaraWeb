using Microsoft.AspNetCore.Identity;

namespace SIARAWEB.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Campos extra requeridos por el RF04
        public string? Name { get; set; }
        public string? Curp { get; set; }
        public string? Rfc { get; set; }
    }
}
