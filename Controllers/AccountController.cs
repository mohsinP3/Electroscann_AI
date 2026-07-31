using ElectroScanAI.Models.Entities;
using ElectroScanAI.Models.Enums;
using Electroscann_ai.Data;
using Electroscann_ai.Models;
using Electroscann_ai.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Electroscann_ai.Controllers
{
    public class AccountController : Controller
    {
        private readonly ElectroscannDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ElectroscannDbContext context,
            IPasswordHasher<User> passwordHasher,
            ILogger<AccountController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        // ========== INDEX GET ==========
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToUserDashboard();
            }
            return RedirectToAction("Login");
        }

        // ========== LOGIN GET ==========
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToUserDashboard();
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // ========== LOGIN POST ==========
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .Include(u => u.ElectricianProfile)
                    .Include(u => u.CompanyProfile)
                    .FirstOrDefaultAsync(u => u.Email == model.Email && !u.IsDeleted);

                if (user != null)
                {
                    // Check Lockout
                    if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                    {
                        var remaining = Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
                        ModelState.AddModelError(string.Empty, $"Account locked due to multiple failed login attempts. Try again in {remaining} minute(s).");
                        return View(model);
                    }

                    if (!user.IsActive)
                    {
                        ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact support.");
                        return View(model);
                    }

                    if (VerifyPassword(user.PasswordHash, model.Password))
                    {
                        // Auto-create missing profiles if necessary
                        if (user.Role == UserRole.Electrician && user.ElectricianProfile == null)
                        {
                            var elec = new Electrician
                            {
                                UserId = user.Id,
                                LicenseNumber = "LIC-PENDING",
                                Specialization = "General Electrical",
                                YearsOfExperience = 1,
                                IsVerified = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Electricians.Add(elec);
                            await _context.SaveChangesAsync();
                            user.ElectricianProfile = elec;
                        }
                        else if (user.Role == UserRole.Company && user.CompanyProfile == null)
                        {
                            var comp = new Company
                            {
                                UserId = user.Id,
                                CompanyName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : "Company",
                                NTNNumber = "NTN-PENDING",
                                Industry = "Electrical Services",
                                IsVerified = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Companies.Add(comp);
                            await _context.SaveChangesAsync();
                            user.CompanyProfile = comp;
                        }

                        // Reset failed attempts & lockout
                        user.FailedLoginAttempts = 0;
                        user.LockoutEnd = null;
                        user.LastLoginAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        // Create claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                            new Claim(ClaimTypes.Name, user.FullName),
                            new Claim(ClaimTypes.Email, user.Email),
                            new Claim(ClaimTypes.Role, user.Role.ToString()),
                            new Claim("UserId", user.Id.ToString()),
                            new Claim("FullName", user.FullName)
                        };

                        // Add role-specific claims
                        if (user.Role == UserRole.Electrician && user.ElectricianProfile != null)
                        {
                            claims.Add(new Claim("ElectricianId", user.ElectricianProfile.Id.ToString()));
                            claims.Add(new Claim("IsVerified", user.ElectricianProfile.IsVerified.ToString()));
                        }
                        else if (user.Role == UserRole.Company && user.CompanyProfile != null)
                        {
                            claims.Add(new Claim("CompanyId", user.CompanyProfile.Id.ToString()));
                            claims.Add(new Claim("CompanyName", user.CompanyProfile.CompanyName));
                        }

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        _logger.LogInformation("User {Email} logged in successfully with role {Role}.", user.Email, user.Role);

                        if (!string.IsNullOrEmpty(returnUrl) 
                            && Url.IsLocalUrl(returnUrl)
                            && !returnUrl.StartsWith("/Account", StringComparison.OrdinalIgnoreCase))
                        {
                            return Redirect(returnUrl);
                        }

                        return RedirectToUserDashboard();
                    }
                    else
                    {
                        // Increment failed login attempts
                        user.FailedLoginAttempts++;
                        if (user.FailedLoginAttempts >= 5)
                        {
                            user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                            _logger.LogWarning("User {Email} locked out due to 5 consecutive failed login attempts.", user.Email);
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }

            return View(model);
        }

        // ========== REGISTER GET ==========
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // ========== REGISTER POST ==========
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Role == UserRole.Electrician)
            {
                if (string.IsNullOrWhiteSpace(model.LicenseNumber))
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.LicenseNumber), "License number is required for electricians.");
                }

                if (string.IsNullOrWhiteSpace(model.Specialization))
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.Specialization), "Specialization is required for electricians.");
                }
            }
            else if (model.Role == UserRole.Company)
            {
                if (string.IsNullOrWhiteSpace(model.CompanyName))
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.CompanyName), "Company name is required for companies.");
                }

                if (string.IsNullOrWhiteSpace(model.NTNNumber))
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.NTNNumber), "NTN number is required for companies.");
                }

                if (string.IsNullOrWhiteSpace(model.Industry))
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.Industry), "Industry is required for companies.");
                }
            }

            var yearsOfExperienceValue = model.YearsOfExperience?.Trim();
            if (!string.IsNullOrWhiteSpace(yearsOfExperienceValue))
            {
                if (!int.TryParse(yearsOfExperienceValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedYears) || parsedYears < 0 || parsedYears > 50)
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.YearsOfExperience), "Years of experience must be a whole number between 0 and 50.");
                }
            }

            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already registered.");
                    return View(model);
                }

                var yearsOfExperience = 0;
                if (int.TryParse(yearsOfExperienceValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
                {
                    yearsOfExperience = parsedValue;
                }

                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    City = model.City ?? "",
                    Role = model.Role,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false,
                    EmailVerified = false
                };

                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    if (model.Role == UserRole.Electrician)
                    {
                        var electrician = new Electrician
                        {
                            UserId = user.Id,
                            LicenseNumber = model.LicenseNumber ?? string.Empty,
                            Specialization = model.Specialization ?? string.Empty,
                            YearsOfExperience = yearsOfExperience,
                            IsVerified = false,
                            CreatedAt = DateTime.UtcNow,
                            Rating = 0,
                            TotalReviews = 0,
                            CompletedJobs = 0
                        };
                        _context.Electricians.Add(electrician);
                    }
                    else if (model.Role == UserRole.Company)
                    {
                        var company = new Company
                        {
                            UserId = user.Id,
                            CompanyName = model.CompanyName ?? string.Empty,
                            NTNNumber = model.NTNNumber ?? string.Empty,
                            Industry = model.Industry ?? string.Empty,
                            IsVerified = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Companies.Add(company);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("User {Email} registered successfully as {Role}.", user.Email, user.Role);

                    TempData["Success"] = "Registration successful! You can now verify your email or login.";
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Registration failed for email {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                }
            }

            return View(model);
        }

        // ========== EMAIL VERIFICATION FLOW ==========
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Invalid email parameter.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Login));
            }

            user.EmailVerified = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email verified for user {Email}", user.Email);
            TempData["Success"] = "Your email has been verified! You can now login.";
            return RedirectToAction(nameof(Login));
        }

        // ========== LOGOUT ==========
        [HttpGet]
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // ========== FORGOT PASSWORD GET ==========
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // ========== FORGOT PASSWORD POST ==========
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email && !u.IsDeleted);
                if (user != null)
                {
                    user.ResetToken = Guid.NewGuid().ToString("N");
                    user.ResetTokenExpiry = DateTime.UtcNow.AddHours(2);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Password reset token generated for user {Email}", user.Email);
                    TempData["Success"] = $"Password reset token generated. Reset Link: /Account/ResetPassword?token={user.ResetToken}";
                    return RedirectToAction(nameof(ResetPassword), new { token = user.ResetToken });
                }
                TempData["Success"] = "If an account exists with that email, a password reset link has been issued.";
            }
            return View(model);
        }

        // ========== RESET PASSWORD GET ==========
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "Invalid or expired password reset token.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);
            if (user == null)
            {
                TempData["Error"] = "Invalid or expired password reset token.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel { Token = token, Email = user.Email };
            return View(model);
        }

        // ========== RESET PASSWORD POST ==========
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == model.Token && u.ResetTokenExpiry > DateTime.UtcNow);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired token.");
                return View(model);
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            user.LockoutEnd = null;
            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password successfully reset for user {Email}", user.Email);
            TempData["Success"] = "Password has been reset successfully! Please login with your new password.";
            return RedirectToAction(nameof(Login));
        }

        // ========== ACCESS DENIED ==========
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ========== HELPER METHODS ==========
        private IActionResult RedirectToUserDashboard()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Dashboard");
            if (User.IsInRole("Electrician"))
                return RedirectToAction("Index", "ElectricianDashboard");
            if (User.IsInRole("Company"))
                return RedirectToAction("Index", "CompanyDashboard");
            if (User.IsInRole("Client"))
                return RedirectToAction("Index", "ClientDashboard");

            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(roleClaim) && Enum.TryParse<UserRole>(roleClaim, true, out var userRole))
            {
                return RedirectToAction(GetDashboardAction(userRole), GetDashboardController(userRole));
            }

            return RedirectToAction("Index", "Home");
        }

        private bool VerifyPassword(string hashedPassword, string password)
        {
            var result = _passwordHasher.VerifyHashedPassword(new User(), hashedPassword, password);
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }

        private string GetDashboardAction(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => "Index",
                UserRole.Electrician => "Index",
                UserRole.Company => "Index",
                UserRole.Client => "Index",
                _ => "Index"
            };
        }

        private string GetDashboardController(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => "Dashboard",
                UserRole.Electrician => "ElectricianDashboard",
                UserRole.Company => "CompanyDashboard",
                UserRole.Client => "ClientDashboard",
                _ => "Home"
            };
        }
    }
}
