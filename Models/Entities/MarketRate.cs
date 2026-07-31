using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class MarketRate : BaseEntity
    {
        [Required]
        [MaxLength(300)]
        public string ItemName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Range(0, 9999999)]
        public decimal Price { get; set; }

        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty;

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;
    }
}