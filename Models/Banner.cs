using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Models
{
    public class Banner
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Image { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Link { get; set; }

        [Required]
        public short Status { get; set; } = 1; // 1=active, 0=inactive

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
