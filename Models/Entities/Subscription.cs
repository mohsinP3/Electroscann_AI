using System.ComponentModel.DataAnnotations;
using ElectroScanAI.Models.Enums;

namespace ElectroScanAI.Models.Entities
{
    public class Subscription : BaseEntity
    {
        public int UserId { get; set; }

        public User? User { get; set; }

        public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(1);

        public bool IsActive { get; set; } = true;
    }
}