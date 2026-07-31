using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Electroscann_ai.Data;
using Electroscann_ai.Services;
using ElectroScanAI.Models.Entities;
using ElectroScanAI.Models.Enums;

namespace Electroscann_ai.Controllers
{
    [Authorize(Roles = "Company")]
    public class CompanyDashboardController : Controller
    {
        private readonly ElectroscannDbContext _context;
        private readonly INotificationService _notificationService;

        public CompanyDashboardController(ElectroscannDbContext context, INotificationService notificationService)
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

        private async Task<Company?> GetCompanyAsync()
        {
            var userId = GetUserId();
            return await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        // ===== INDEX =====
        public async Task<IActionResult> Index()
        {
            var company = await GetCompanyAsync();
            if (company == null) return RedirectToAction("Login", "Account");

            ViewBag.CompanyId = company.Id;
            ViewBag.TotalJobs = await _context.Jobs.CountAsync(j => j.CompanyId == company.Id);
            ViewBag.OpenJobs = await _context.Jobs.CountAsync(j => j.CompanyId == company.Id && j.Status == JobStatus.Open);
            ViewBag.PendingApplications = await _context.JobApplications
                .CountAsync(ja => ja.Job!.CompanyId == company.Id && ja.Status == "Pending");
            var paidPayments = await _context.Payments
                .Where(p => p.UserId == GetUserId() && p.Status == PaymentStatus.Paid)
                .ToListAsync();
            ViewBag.TotalPayments = paidPayments.Sum(p => p.Amount);

            // Recent jobs
            ViewBag.RecentJobs = await _context.Jobs
                .Where(j => j.CompanyId == company.Id)
                .OrderByDescending(j => j.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // ===== PROFILE =====
        public async Task<IActionResult> Profile()
        {
            var userId = GetUserId();
            var user = await _context.Users
                .Include(u => u.CompanyProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber, string city,
            string companyName, string industry, string ntnNumber)
        {
            var userId = GetUserId();
            var user = await _context.Users
                .Include(u => u.CompanyProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.City = city;

            if (user.CompanyProfile != null)
            {
                user.CompanyProfile.CompanyName = companyName;
                user.CompanyProfile.Industry = industry;
                user.CompanyProfile.NTNNumber = ntnNumber;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // ===== JOBS LIST =====
        public async Task<IActionResult> Jobs()
        {
            var company = await GetCompanyAsync();
            if (company == null) return View(new System.Collections.Generic.List<Job>());

            var jobs = await _context.Jobs
                .Where(j => j.CompanyId == company.Id)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return View(jobs);
        }

        // ===== CREATE JOB GET =====
        [HttpGet]
        public IActionResult CreateJob() => View();

        // ===== CREATE JOB POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJob(string title, string description, decimal budget,
            string location)
        {
            var company = await GetCompanyAsync();
            if (company == null) return NotFound();

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("", "Job title is required.");
                return View();
            }

            var job = new Job
            {
                Title = title,
                Description = description,
                Budget = budget,
                Location = location,
                CompanyId = company.Id,
                Status = JobStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Job posted successfully!";
            return RedirectToAction(nameof(Jobs));
        }

        // ===== EDIT JOB GET =====
        [HttpGet]
        public async Task<IActionResult> EditJob(int id)
        {
            var company = await GetCompanyAsync();
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.CompanyId == company!.Id);
            if (job == null) return NotFound();
            return View(job);
        }

        // ===== EDIT JOB POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(int id, string title, string description,
            decimal budget, string location, JobStatus status)
        {
            var company = await GetCompanyAsync();
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.CompanyId == company!.Id);
            if (job == null) return NotFound();

            job.Title = title;
            job.Description = description;
            job.Budget = budget;
            job.Location = location;
            job.Status = status;
            job.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Job updated successfully.";
            return RedirectToAction(nameof(Jobs));
        }

        // ===== DELETE JOB =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var company = await GetCompanyAsync();
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.CompanyId == company!.Id);
            if (job == null) return NotFound();

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Job deleted.";
            return RedirectToAction(nameof(Jobs));
        }

        // ===== APPLICATIONS =====
        public async Task<IActionResult> Applications(int? jobId = null)
        {
            var company = await GetCompanyAsync();
            if (company == null) return View(new System.Collections.Generic.List<JobApplication>());

            var query = _context.JobApplications
                .Include(ja => ja.Job)
                .Include(ja => ja.Electrician)
                    .ThenInclude(e => e!.User)
                .Where(ja => ja.Job!.CompanyId == company.Id);

            if (jobId.HasValue)
                query = query.Where(ja => ja.JobId == jobId.Value);

            var applications = await query.OrderByDescending(ja => ja.CreatedAt).ToListAsync();
            ViewBag.FilterJobId = jobId;
            return View(applications);
        }

        // ===== ACCEPT / REJECT APPLICATION =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateApplicationStatus(int applicationId, string status)
        {
            var company = await GetCompanyAsync();
            var app = await _context.JobApplications
                .Include(ja => ja.Job)
                .FirstOrDefaultAsync(ja => ja.Id == applicationId && ja.Job!.CompanyId == company!.Id);

            if (app == null) return NotFound();

            app.Status = status;
            app.UpdatedAt = DateTime.UtcNow;

            if (status == "Accepted" && app.Job != null)
            {
                app.Job.Status = JobStatus.InProgress;
            }

            await _context.SaveChangesAsync();

            if (app.Electrician != null && app.Electrician.UserId > 0 && (status == "Accepted" || status == "Rejected"))
            {
                await _notificationService.NotifyAsync(
                    app.Electrician.UserId,
                    status == "Accepted" ? "Application accepted" : "Application rejected",
                    status == "Accepted"
                        ? $"Your application for '{app.Job?.Title ?? "the job"}' was accepted."
                        : $"Your application for '{app.Job?.Title ?? "the job"}' was rejected.");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Application {status.ToLower()} successfully.";
            return RedirectToAction(nameof(Applications));
        }

        // ===== PAYMENTS =====
        public async Task<IActionResult> Payments()
        {
            var userId = GetUserId();
            var payments = await _context.Payments
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.TotalSpent = payments.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount);
            return View(payments);
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