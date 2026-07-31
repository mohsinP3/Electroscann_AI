using System;
using System.Linq;
using System.Threading.Tasks;
using ElectroScanAI.Models.Entities;
using Electroscann_ai.Data;
using Electroscann_ai.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Electroscann_ai.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ElectroscannDbContext _context;
        private readonly INotificationService _notificationService;

        public ReviewController(ElectroscannDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int electricianId, int rating, string comment)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int reviewerId) || reviewerId <= 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var electrician = await _context.Electricians.FindAsync(electricianId);
            if (electrician == null) return NotFound();

            var review = new Review
            {
                ReviewerId = reviewerId,
                ElectricianId = electricianId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var reviews = await _context.Reviews.Where(r => r.ElectricianId == electricianId).ToListAsync();
            electrician.TotalReviews = reviews.Count;
            electrician.Rating = reviews.Average(r => r.Rating);
            await _context.SaveChangesAsync();

            if (electrician.UserId > 0)
            {
                await _notificationService.NotifyAsync(
                    electrician.UserId,
                    "New review received",
                    "You received a new client review on your profile.");
            }

            TempData["Success"] = "Review submitted successfully!";
            string referer = Request.Headers["Referer"].ToString();
            if (string.IsNullOrWhiteSpace(referer)) referer = "/";
            return Redirect(referer);
        }
    }
}
