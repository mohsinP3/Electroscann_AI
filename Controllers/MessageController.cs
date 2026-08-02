using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectroScanAI.Models.Entities;
using ElectroScanAI.Models.Enums;
using Electroscann_ai.Data;
using Electroscann_ai.Services;
using Electroscann_ai.Models.ViewModels;
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
        public async Task<IActionResult> Inbox(int? selectedUserId = null)
        {
            var currentUserId = GetUserId();
            if (currentUserId <= 0) return RedirectToAction("Login", "Account");

            // 1. Load all messages involving the current user
            var allMessages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == currentUserId || m.SenderId == currentUserId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            // 2. Group messages by the conversation partner
            var conversationsGrouped = allMessages
                .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .ToList();

            var conversationItems = new List<ConversationItem>();

            foreach (var group in conversationsGrouped)
            {
                var partnerId = group.Key;
                // Find partner User record
                var partner = await _context.Users
                    .Include(u => u.CompanyProfile)
                    .Include(u => u.ElectricianProfile)
                    .FirstOrDefaultAsync(u => u.Id == partnerId);

                if (partner == null) continue;

                var lastMsg = group.OrderByDescending(m => m.SentAt).First();
                var unreadCount = group.Count(m => m.ReceiverId == currentUserId && !m.IsRead);

                conversationItems.Add(new ConversationItem
                {
                    Partner = partner,
                    PartnerRole = partner.Role.ToString(),
                    LastMessage = lastMsg,
                    UnreadCount = unreadCount,
                    IsSelected = selectedUserId.HasValue && selectedUserId.Value == partnerId
                });
            }

            // Order conversations by the most recent message
            conversationItems = conversationItems.OrderByDescending(c => c.LastMessage.SentAt).ToList();

            // If no conversation is specifically selected but we have active conversations, select the first one
            if (!selectedUserId.HasValue && conversationItems.Any())
            {
                selectedUserId = conversationItems.First().Partner.Id;
                conversationItems.First().IsSelected = true;
            }

            var activeMessages = new List<Message>();
            User? selectedPartner = null;

            if (selectedUserId.HasValue)
            {
                selectedPartner = await _context.Users
                    .Include(u => u.CompanyProfile)
                    .Include(u => u.ElectricianProfile)
                    .FirstOrDefaultAsync(u => u.Id == selectedUserId.Value);

                if (selectedPartner != null)
                {
                    // Mark messages from this partner as read
                    var unread = allMessages
                        .Where(m => m.SenderId == selectedUserId.Value && m.ReceiverId == currentUserId && !m.IsRead)
                        .ToList();

                    if (unread.Any())
                    {
                        foreach (var m in unread)
                        {
                            m.IsRead = true;
                        }
                        await _context.SaveChangesAsync();
                    }

                    // Load thread messages in chronological order (oldest first)
                    activeMessages = await _context.Messages
                        .Include(m => m.Sender)
                        .Include(m => m.Receiver)
                        .Where(m => (m.SenderId == currentUserId && m.ReceiverId == selectedUserId.Value) ||
                                    (m.SenderId == selectedUserId.Value && m.ReceiverId == currentUserId))
                        .OrderBy(m => m.SentAt)
                        .ToListAsync();
                }
            }

            // 3. Determine Eligible Contacts based on active relationships and roles
            var currentUser = await _context.Users
                .Include(u => u.CompanyProfile)
                .Include(u => u.ElectricianProfile)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            var eligibleContacts = new List<User>();

            if (currentUser != null)
            {
                // We want to fetch users they can message.
                // Clients can message Electricians and Companies they are connected to.
                // Electricians can message Companies they have applications with.
                // Let's fetch relevant users based on JobApplications, Reviews, etc.
                var relatedUserIds = new HashSet<int>();

                if (currentUser.Role == UserRole.Client)
                {
                    // Find companies linked through any of their payments or calculations
                    // or find any company/electrician who has left reviews
                    var reviews = await _context.Reviews
                        .Where(r => r.ReviewerId == currentUserId)
                        .Select(r => r.ElectricianId)
                        .ToListAsync();

                    var reviewUserIds = await _context.Electricians
                        .Where(e => reviews.Contains(e.Id))
                        .Select(e => e.UserId)
                        .ToListAsync();

                    foreach (var rId in reviewUserIds) relatedUserIds.Add(rId);
                }
                else if (currentUser.Role == UserRole.Electrician)
                {
                    if (currentUser.ElectricianProfile != null)
                    {
                        var appCompanyUserIds = await _context.JobApplications
                            .Include(ja => ja.Job)
                                .ThenInclude(j => j!.Company)
                            .Where(ja => ja.ElectricianId == currentUser.ElectricianProfile.Id && ja.Job != null && ja.Job.Company != null)
                            .Select(ja => ja.Job!.Company!.UserId)
                            .Distinct()
                            .ToListAsync();

                        foreach (var cId in appCompanyUserIds) relatedUserIds.Add(cId);
                    }
                }
                else if (currentUser.Role == UserRole.Company)
                {
                    if (currentUser.CompanyProfile != null)
                    {
                        var applicantElectricianUserIds = await _context.JobApplications
                            .Include(ja => ja.Job)
                            .Include(ja => ja.Electrician)
                            .Where(ja => ja.Job != null && ja.Job.CompanyId == currentUser.CompanyProfile.Id && ja.Electrician != null)
                            .Select(ja => ja.Electrician!.UserId)
                            .Distinct()
                            .ToListAsync();

                        foreach (var eId in applicantElectricianUserIds) relatedUserIds.Add(eId);
                    }
                }

                // Query users who have relationships
                var relationshipContacts = await _context.Users
                    .Where(u => u.Id != currentUserId && !u.IsDeleted && u.Role != UserRole.Admin)
                    .Where(u => relatedUserIds.Contains(u.Id))
                    .ToListAsync();

                eligibleContacts.AddRange(relationshipContacts);

                // Add other active Users as fallback contacts (ordered by relationship, then other active accounts)
                var fallbackContacts = await _context.Users
                    .Where(u => u.Id != currentUserId && !u.IsDeleted && u.Role != UserRole.Admin)
                    .Where(u => !relatedUserIds.Contains(u.Id))
                    .ToListAsync();

                eligibleContacts.AddRange(fallbackContacts);
            }

            var viewModel = new InboxViewModel
            {
                Conversations = conversationItems,
                SelectedPartner = selectedPartner,
                Messages = activeMessages,
                EligibleContacts = eligibleContacts
            };

            ViewBag.ActivePage = "Messages";
            return View(viewModel);
        }

        // POST /Message/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int receiverId, string messageText, bool isAjax = false)
        {
            var senderId = GetUserId();
            if (senderId <= 0)
            {
                if (isAjax) return Json(new { success = false, message = "Not authenticated." });
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(messageText))
            {
                if (isAjax) return Json(new { success = false, message = "Message text cannot be empty." });
                TempData["Error"] = "Message text cannot be empty.";
                return RedirectToAction(nameof(Inbox), new { selectedUserId = receiverId });
            }

            // Verify receiver exists
            var receiver = await _context.Users.FindAsync(receiverId);
            if (receiver == null)
            {
                if (isAjax) return Json(new { success = false, message = "Recipient not found." });
                TempData["Error"] = "Recipient not found.";
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

            // Trigger notification
            await _notificationService.NotifyAsync(
                receiverId,
                "New message from " + User.Identity?.Name,
                messageText.Length > 60 ? messageText.Substring(0, 57) + "..." : messageText,
                "Message"
            );

            if (isAjax)
            {
                return Json(new
                {
                    success = true,
                    messageId = message.Id,
                    messageText = message.MessageText,
                    sentAt = message.SentAt.ToString("g"),
                    senderId = message.SenderId,
                    senderName = User.Identity?.Name ?? "Me"
                });
            }

            TempData["Success"] = "Message sent successfully!";
            return RedirectToAction(nameof(Inbox), new { selectedUserId = receiverId });
        }
    }
}
