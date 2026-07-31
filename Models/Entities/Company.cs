using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(300)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string NTNNumber { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Industry { get; set; } = string.Empty;

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Logo { get; set; }

        // Navigation
        public User? User { get; set; }
    }
}