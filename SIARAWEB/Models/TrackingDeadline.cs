using System;
using System.ComponentModel.DataAnnotations;

namespace SIARAWEB.Models
{
    public class TrackingDeadline
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un seguimiento")]
        public string Phase { get; set; } // Aquí guardaremos "Primer Seguimiento", etc.

        [Required(ErrorMessage = "La fecha de corte es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime CutoffDate { get; set; } // Aquí se guarda la fecha del calendario
    }
}