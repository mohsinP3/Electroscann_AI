using ElectroScanAI.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public bool EmailVerified { get; set; } = false;

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockoutEnd { get; set; }

        [MaxLength(200)]
        public string? ResetToken { get; set; }

        public DateTime? ResetTokenExpiry { get; set; }

        [MaxLength(500)]
        public string? ProfileImage { get; set; }

        // Navigation
        public Electrician? ElectricianProfile { get; set; }
        public Company? CompanyProfile { get; set; }
    }
}