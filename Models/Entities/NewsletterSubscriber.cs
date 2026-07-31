using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class NewsletterSubscriber : BaseEntity
    {
        [Required]
        [EmailAddress]
        [MaxLength(300)]
        public string Email { get; set; } = string.Empty;

        public bool IsSubscribed { get; set; } = true;
    }
}
