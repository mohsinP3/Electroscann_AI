using System.ComponentModel.DataAnnotations;
using ElectroScanAI.Models.Enums;

namespace ElectroScanAI.Models.Entities
{
    public class AIScan : BaseEntity
    {
        [Required]
        [MaxLength(300)]
        public string ScanName { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public User? User { get; set; }

        [MaxLength(500)]
        public string ImagePath { get; set; } = string.Empty;

        public ScanRiskLevel RiskLevel { get; set; } = ScanRiskLevel.Low;

        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // Analysis result JSON or text
        public string? ResultSummary { get; set; }
    }
}