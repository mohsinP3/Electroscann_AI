using System.ComponentModel.DataAnnotations;

namespace Electroscann_ai.Models.ViewModels
{
    public class WireCalculatorViewModel
    {
        // ── Room dimensions (drive Distance) ──
        [Required(ErrorMessage = "Room length is required")]
        [Range(1, 500, ErrorMessage = "Length must be between 1 and 500 ft")]
        [Display(Name = "Length (ft)")]
        public double RoomLength { get; set; } = 14;

        [Required(ErrorMessage = "Room width is required")]
        [Range(1, 500, ErrorMessage = "Width must be between 1 and 500 ft")]
        [Display(Name = "Width (ft)")]
        public double RoomWidth { get; set; } = 12;

        [Required(ErrorMessage = "Room height is required")]
        [Range(7, 30, ErrorMessage = "Height must be between 7 and 30 ft")]
        [Display(Name = "Height (ft)")]
        public double RoomHeight { get; set; } = 9;

        // ── Fixture inputs (drive Load) ──
        [Required(ErrorMessage = "Lights count is required")]
        [Range(0, 30, ErrorMessage = "Lights must be between 0 and 30")]
        [Display(Name = "Lights")]
        public int Lights { get; set; } = 6;

        [Required(ErrorMessage = "Fans count is required")]
        [Range(0, 30, ErrorMessage = "Fans must be between 0 and 30")]
        [Display(Name = "Fans")]
        public int Fans { get; set; } = 2;

        [Required(ErrorMessage = "Sockets count is required")]
        [Range(0, 30, ErrorMessage = "Sockets must be between 0 and 30")]
        [Display(Name = "Sockets")]
        public int Sockets { get; set; } = 6;

        [Required(ErrorMessage = "AC units count is required")]
        [Range(0, 30, ErrorMessage = "AC units must be between 0 and 30")]
        [Display(Name = "AC Units")]
        public int AcUnits { get; set; } = 1;

        [Required(ErrorMessage = "Heavy loads count is required")]
        [Range(0, 30, ErrorMessage = "Heavy loads must be between 0 and 30")]
        [Display(Name = "Heavy Loads")]
        public int HeavyLoads { get; set; } = 0;

        // ── Computed results (server-populated; Range guards against bad saves) ──
        [Range(0, 200000, ErrorMessage = "Load must be between 0 and 200000 W")]
        [Display(Name = "Total Load (W)")]
        public double Load { get; set; }

        [Range(0, 100000, ErrorMessage = "Distance must be between 0 and 100000 m")]
        [Display(Name = "Wire Distance (m)")]
        public double Distance { get; set; }

        public string? WireSize { get; set; }
        public string? Result { get; set; }
        public double CurrentAmps { get; set; }
        public string? BreakerRating { get; set; }
        public double RoomArea { get; set; }
        public string? SafetyMessage { get; set; }
        public bool HasResult { get; set; }

        /// <summary>Last 5 saved calculations — populated only for authenticated users.</summary>
        public List<WireCalculationHistoryItem>? RecentCalculations { get; set; }
    }

    public class WireCalculationHistoryItem
    {
        public int Id { get; set; }
        public double Load { get; set; }
        public double Distance { get; set; }
        public string WireSize { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
