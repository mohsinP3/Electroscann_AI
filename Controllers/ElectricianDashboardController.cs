using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using Electroscann_ai.Data;
using Electroscann_ai.Services;
using ElectroScanAI.Models.Entities;
using ElectroScanAI.Models.Enums;

namespace Electroscann_ai.Controllers
{
    [Authorize(Roles = "Electrician")]
    public class ElectricianDashboardController : Controller
    {
        private readonly ElectroscannDbContext _context;
        private readonly INotificationService _notificationService;

        public ElectricianDashboardController(ElectroscannDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userId = GetUserId();
            ViewBag.UnreadNotifications = userId > 0
                ? await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead)
                : 0;
            await base.OnActionExecutionAsync(context, next);
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst("UserId")?.Value, out int id) ? id : 0;

        // ===== INDEX (Dashboard Home) =====
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var electrician = await _context.Electricians
                .FirstOrDefaultAsync(e => e.UserId == userId);

            ViewBag.ElectricianId = electrician?.Id ?? 0;
            ViewBag.AvailableJobs = await _context.Jobs.CountAsync(j => j.Status == JobStatus.Open);
            ViewBag.CompletedJobs = electrician?.CompletedJobs ?? 0;
            ViewBag.Rating = electrician?.Rating.ToString("0.0") ?? "0.0";
            ViewBag.TotalReviews = electrician?.TotalReviews ?? 0;
            ViewBag.IsVerified = electrician?.IsVerified ?? false;

            var payments = await _context.Payments
                .Where(p => p.UserId == userId)
                .ToListAsync();

            ViewBag.TotalEarnings = payments
                .Where(p => p.Status == PaymentStatus.Paid)
                .Sum(p => p.Amount);

            // Recent applied jobs
            if (electrician != null)
            {
                ViewBag.RecentApplications = await _context.JobApplications
                    .Include(ja => ja.Job)
                    .Where(ja => ja.ElectricianId == electrician.Id)
                    .OrderByDescending(ja => ja.CreatedAt)
                    .Take(5)
                    .ToListAsync();
            }

            return View();
        }

        // ===== PROFILE =====
        public async Task<IActionResult> Profile()
        {
            var userId = GetUserId();
            var user = await _context.Users
                .Include(u => u.ElectricianProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber, string city,
            string specialization, int yearsOfExperience)
        {
            var userId = GetUserId();
            var user = await _context.Users
                .Include(u => u.ElectricianProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.City = city;

            if (user.ElectricianProfile != null)
            {
                user.ElectricianProfile.Specialization = specialization;
                user.ElectricianProfile.YearsOfExperience = yearsOfExperience;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // ===== AVAILABLE JOBS =====
        public async Task<IActionResult> AvailableJobs(string? search, string? location)
        {
            var query = _context.Jobs
                .Include(j => j.Company)
                .Where(j => j.Status == JobStatus.Open);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(j => j.Title.Contains(search) || j.Description.Contains(search));

            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(j => j.Location.Contains(location));

            var jobs = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();
            return View(jobs);
        }

        // ===== APPLY FOR JOB =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyForJob(int jobId, string coverLetter)
        {
            var userId = GetUserId();
            var electrician = await _context.Electricians
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (electrician == null)
            {
                TempData["Error"] = "Electrician profile not found.";
                return RedirectToAction(nameof(AvailableJobs));
            }

            // Check if already applied
            var existingApp = await _context.JobApplications
                .FirstOrDefaultAsync(ja => ja.JobId == jobId && ja.ElectricianId == electrician.Id);

            if (existingApp != null)
            {
                TempData["Error"] = "You have already applied for this job.";
                return RedirectToAction(nameof(AvailableJobs));
            }

            var application = new JobApplication
            {
                JobId = jobId,
                ElectricianId = electrician.Id,
                CoverLetter = coverLetter,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            if (application.JobId > 0)
            {
                var job = await _context.Jobs.Include(j => j.Company).FirstOrDefaultAsync(j => j.Id == application.JobId);
                if (job?.Company?.UserId > 0)
                {
                    await _notificationService.NotifyAsync(
                        job.Company.UserId,
                        "New job application",
                        $"A new electrician application was received for '{job.Title}'.");
                }
            }

            TempData["Success"] = "Application submitted successfully!";
            return RedirectToAction(nameof(AppliedJobs));
        }

        // ===== APPLIED JOBS =====
        public async Task<IActionResult> AppliedJobs()
        {
            var userId = GetUserId();
            var electrician = await _context.Electricians
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (electrician == null) return View(new System.Collections.Generic.List<JobApplication>());

            var applications = await _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Where(ja => ja.ElectricianId == electrician.Id)
                .OrderByDescending(ja => ja.CreatedAt)
                .ToListAsync();

            return View(applications);
        }

        // ===== PROJECTS (completed jobs) =====
        public async Task<IActionResult> Projects()
        {
            var userId = GetUserId();
            var electrician = await _context.Electricians
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (electrician == null) return View(new System.Collections.Generic.List<JobApplication>());

            var completedJobs = await _context.JobApplications
                .Include(ja => ja.Job)
                    .ThenInclude(j => j!.Company)
                .Where(ja => ja.ElectricianId == electrician.Id && ja.Status == "Accepted")
                .OrderByDescending(ja => ja.CreatedAt)
                .ToListAsync();

            return View(completedJobs);
        }

        // ===== EARNINGS =====
        public async Task<IActionResult> Earnings()
        {
            var userId = GetUserId();
            var payments = await _context.Payments
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.TotalEarnings = payments
                .Where(p => p.Status == PaymentStatus.Paid)
                .Sum(p => p.Amount);

            return View(payments);
        }

        // ===== MESSAGES =====
        public async Task<IActionResult> Messages()
        {
            var userId = GetUserId();
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == userId || m.SenderId == userId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            ViewBag.Users = await _context.Users
                .Where(u => u.Id != userId && !u.IsDeleted)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int receiverId, string messageText)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(messageText))
            {
                TempData["Error"] = "Message cannot be empty.";
                return RedirectToAction(nameof(Messages));
            }

            var message = new Message
            {
                SenderId = userId,
                ReceiverId = receiverId,
                MessageText = messageText,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Message sent!";
            return RedirectToAction(nameof(Messages));
        }

        // ===== NOTIFICATIONS =====
        public async Task<IActionResult> Notifications()
        {
            var userId = GetUserId();
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Mark all as read
            var unread = notifications.Where(n => !n.IsRead).ToList();
            unread.ForEach(n => n.IsRead = true);
            if (unread.Any()) await _context.SaveChangesAsync();

            return View(notifications);
        }

        // ===== SETTINGS =====
        public async Task<IActionResult> Settings()
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New passwords do not match.";
                return RedirectToAction(nameof(Settings));
            }

            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // Verify current password using the same hasher approach as AccountController
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
            var verifyResult = hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            if (verifyResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            {
                TempData["Error"] = "Current password is incorrect.";
                return RedirectToAction(nameof(Settings));
            }

            user.PasswordHash = hasher.HashPassword(user, newPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(Settings));
        }
    }
}