using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class Electrician
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Specialization { get; set; } = string.Empty;

        public int YearsOfExperience { get; set; }

        public bool IsVerified { get; set; } = false;

        public double Rating { get; set; } = 0;

        public int TotalReviews { get; set; } = 0;

        public int CompletedJobs { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? User { get; set; }
    }
}