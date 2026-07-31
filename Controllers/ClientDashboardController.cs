using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Electroscann_ai.Data;
using ElectroScanAI.Models.Entities;
using ElectroScanAI.Models.Enums;

namespace Electroscann_ai.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientDashboardController : Controller
    {
        private readonly ElectroscannDbContext _context;

        public ClientDashboardController(ElectroscannDbContext context)
        {
            _context = context;
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

        // ===== INDEX =====
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();

            ViewBag.TotalScans = await _context.AIScans.CountAsync(s => s.UserId == userId);
            ViewBag.TotalCalculations = await _context.WireCalculations.CountAsync(w => w.UserId == userId);
            var paidPayments = await _context.Payments
                .Where(p => p.UserId == userId && p.Status == PaymentStatus.Paid)
                .ToListAsync();
            ViewBag.TotalPayments = paidPayments.Sum(p => p.Amount);

            // Recent scans
            ViewBag.RecentScans = await _context.AIScans
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // ===== PROFILE =====
        public async Task<IActionResult> Profile()
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber, string city)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.City = city;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // ===== PROJECTS (AI Scans) =====
        public async Task<IActionResult> Projects()
        {
            var userId = GetUserId();
            var scans = await _context.AIScans
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            ViewBag.DefaultElectricianId = await _context.Electricians.OrderBy(e => e.Id).Select(e => e.Id).FirstOrDefaultAsync();
            ViewBag.ReviewedElectricianIds = await _context.Reviews
                .Where(r => r.ReviewerId == userId)
                .Select(r => r.ElectricianId)
                .Distinct()
                .ToListAsync();
            return View(scans);
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