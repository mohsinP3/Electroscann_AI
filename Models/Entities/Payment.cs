using System.ComponentModel.DataAnnotations;
using ElectroScanAI.Models.Enums;

namespace ElectroScanAI.Models.Entities
{
    public class Payment : BaseEntity
    {
        public int UserId { get; set; }

        public User? User { get; set; }

        [Range(0, 9999999)]
        public decimal Amount { get; set; }

        [MaxLength(200)]
        public string TransactionId { get; set; } = string.Empty;

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? PaymentMethod { get; set; }

        // Optional link to subscription
        public int? SubscriptionId { get; set; }
    }
}