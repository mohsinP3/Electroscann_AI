using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class Notification : BaseEntity
    {
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        [MaxLength(50)]
        public string Type { get; set; } = "Info"; // Info, Success, Warning, Danger
    }
}