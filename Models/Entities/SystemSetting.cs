using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class SystemSetting : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
