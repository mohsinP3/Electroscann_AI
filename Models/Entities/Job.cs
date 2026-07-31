using System.ComponentModel.DataAnnotations;
using ElectroScanAI.Models.Enums;

namespace ElectroScanAI.Models.Entities
{
    public class Job : BaseEntity
    {
        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Range(0, 9999999)]
        public decimal Budget { get; set; }

        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        public JobStatus Status { get; set; } = JobStatus.Open;

        public int CompanyId { get; set; }

        public Company? Company { get; set; }
    }
}