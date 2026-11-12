using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIARAWEB.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(250)]
        public string Message { get; set; } = string.Empty;

        [StringLength(250)]
        public string? ActionUrl { get; set; }

        public bool IsRead { get; set; } = false;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "Normal"; // "Normal" or "Importante"

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
