using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class Message : BaseEntity
    {
        public int SenderId { get; set; }

        public User? Sender { get; set; }

        public int ReceiverId { get; set; }

        public User? Receiver { get; set; }

        [Required]
        public string MessageText { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}