using ElectroScanAI.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Electroscann_ai.Models.ViewModels
{
    public class RegisterViewModel
    {
        // Common Fields
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100, MinimumLength = 3)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a role")]
        public UserRole Role { get; set; }

        // Electrician Specific Fields
        [Display(Name = "License Number")]
        public string? LicenseNumber { get; set; }

        [Display(Name = "Specialization")]
        public string? Specialization { get; set; }

        [Display(Name = "Years of Experience")]
        public string? YearsOfExperience { get; set; }

        // Company Specific Fields
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Display(Name = "NTN Number")]
        public string? NTNNumber { get; set; }

        [Display(Name = "Industry")]
        public string? Industry { get; set; }
    }
}