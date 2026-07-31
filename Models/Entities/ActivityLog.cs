using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class ActivityLog : BaseEntity
    {
        public int UserId { get; set; }

        public User? User { get; set; }

        [MaxLength(200)]
        public string Action { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        public string IPAddress { get; set; } = string.Empty;
    }
}