using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;

namespace Plafind.Services
{
    public interface INotificationService
    {
        Task<bool> SendNotificationAsync(string userId, string title, string message, string type = "Info", string category = "General", string? actionUrl = null, string? actionText = null, int? relatedEntityId = null, string? relatedEntityType = null);
        Task<bool> SendReservationNotificationAsync(string userId, int reservationId, string message);
        Task<bool> SendReviewNotificationAsync(string userId, int reviewId, string message);
        Task<bool> SendPaymentNotificationAsync(string userId, int paymentId, string message);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int? take = null);
        Task<int> GetUnreadCountAsync(string userId);
        Task<bool> MarkAsReadAsync(int notificationId, string userId);
        Task<bool> MarkAllAsReadAsync(string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;

        public NotificationService(ApplicationDbContext context, IEmailService emailService, ISmsService smsService)
        {
            _context = context;
            _emailService = emailService;
            _smsService = smsService;
        }

        public async Task<bool> SendNotificationAsync(string userId, string title, string message, string type = "Info", string category = "General", string? actionUrl = null, string? actionText = null, int? relatedEntityId = null, string? relatedEntityType = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Category = category,
                    ActionUrl = actionUrl,
                    ActionText = actionText,
                    RelatedEntityId = relatedEntityId,
                    RelatedEntityType = relatedEntityType,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Kullanıcı tercihlerine göre email/SMS gönder
                var preference = await _context.NotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (preference == null || preference.EmailEnabled)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        await _emailService.SendNotificationEmailAsync(user.Email, title, message);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendReservationNotificationAsync(string userId, int reservationId, string message)
        {
            return await SendNotificationAsync(
                userId,
                "Rezervasyon Bildirimi",
                message,
                "Info",
                "Reservation",
                $"/Reservations/Details/{reservationId}",
                "Rezervasyonu Görüntüle",
                reservationId,
                "Reservation"
            );
        }

        public async Task<bool> SendReviewNotificationAsync(string userId, int reviewId, string message)
        {
            return await SendNotificationAsync(
                userId,
                "Yorum Bildirimi",
                message,
                "Info",
                "Review",
                $"/Reviews/Details/{reviewId}",
                "Yorumu Görüntüle",
                reviewId,
                "Review"
            );
        }

        public async Task<bool> SendPaymentNotificationAsync(string userId, int paymentId, string message)
        {
            return await SendNotificationAsync(
                userId,
                "Ödeme Bildirimi",
                message,
                "Success",
                "Payment",
                $"/Payments/Details/{paymentId}",
                "Ödemeyi Görüntüle",
                paymentId,
                "Payment"
            );
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int? take = null)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            query = query.OrderByDescending(n => n.CreatedDate);

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadDate = DateTime.Now;
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;
            }

            _context.Notifications.UpdateRange(notifications);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
