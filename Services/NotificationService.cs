using System;
using System.Threading.Tasks;
using ElectroScanAI.Models.Entities;
using Electroscann_ai.Data;

namespace Electroscann_ai.Services
{
    public interface INotificationService
    {
        Task NotifyAsync(int userId, string title, string message, string type = "Info");
    }

    public class NotificationService : INotificationService
    {
        private readonly ElectroscannDbContext _context;

        public NotificationService(ElectroscannDbContext context)
        {
            _context = context;
        }

        public async Task NotifyAsync(int userId, string title, string message, string type = "Info")
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}
