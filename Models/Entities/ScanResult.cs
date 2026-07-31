using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class ScanResult : BaseEntity
    {
        public int AIScanId { get; set; }

        public AIScan? AIScan { get; set; }

        [MaxLength(500)]
        public string DetectedIssue { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Severity { get; set; } = "Low";

        public string Recommendation { get; set; } = string.Empty;
    }
}