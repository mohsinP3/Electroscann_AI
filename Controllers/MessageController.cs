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
    public class MessageController : Controller
    {
        private readonly ElectroscannDbContext _context;
        private readonly INotificationService _notificationService;

        public MessageController(ElectroscannDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst("UserId")?.Value, out int id) ? id : 0;

        // GET /Message/Inbox
        [HttpGet]
        public async Task<IActionResult> Inbox()
        {
            var userId = GetUserId();
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == userId || m.SenderId == userId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return View(messages);
        }

        // POST /Message/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int receiverId, string messageText)
        {
            var senderId = GetUserId();
            if (senderId <= 0) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(messageText))
            {
                TempData["Error"] = "Message text cannot be empty.";
                return RedirectToAction(nameof(Inbox));
            }

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                MessageText = messageText,
                IsRead = false,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            if (receiverId > 0)
            {
                await _notificationService.NotifyAsync(
                    receiverId,
                    "New message",
                    "You received a new message in your inbox.");
            }

            TempData["Success"] = "Message sent successfully!";
            return RedirectToAction(nameof(Inbox));
        }
    }
}
