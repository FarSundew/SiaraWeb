using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace SIARAWEB.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }
        public string? Curp { get; set; }
        public string? Rfc { get; set; }

        // Propiedad de navegación agregada
        public List<DocenteAsignatura> DocenteAsignaturas { get; set; } = new List<DocenteAsignatura>();
    }
}