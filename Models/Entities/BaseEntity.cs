using System;
using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}