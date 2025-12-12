using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;
using System.Security.Claims;

namespace Plafind.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            // Kullanıcının konuşmalarını getir
            var conversations = await _context.Conversations
                .Where(c => (c.User1Id == userId || c.User2Id == userId) &&
                           (c.User1Id == userId ? !c.IsArchivedByUser1 : !c.IsArchivedByUser2))
                .Include(c => c.User1)
                .Include(c => c.User2)
                .OrderByDescending(c => c.LastMessageDate)
                .ToListAsync();

            // Her konuşma için son mesajı ve okunmamış mesaj sayısını getir
            var conversationsWithDetails = new List<object>();
            foreach (var conv in conversations)
            {
                var otherUserId = conv.User1Id == userId ? conv.User2Id : conv.User1Id;
                var lastMessage = await _context.Messages
                    .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                               (m.SenderId == otherUserId && m.ReceiverId == userId))
                    .Where(m => !(m.SenderId == userId && m.IsDeletedBySender) &&
                               !(m.ReceiverId == userId && m.IsDeletedByReceiver))
                    .OrderByDescending(m => m.CreatedDate)
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.Messages
                    .CountAsync(m => m.ReceiverId == userId &&
                                   m.SenderId == otherUserId &&
                                   !m.IsRead &&
                                   !m.IsDeletedByReceiver);

                conversationsWithDetails.Add(new
                {
                    Conversation = conv,
                    OtherUser = conv.User1Id == userId ? conv.User2 : conv.User1,
                    LastMessage = lastMessage,
                    UnreadCount = unreadCount
                });
            }

            ViewBag.Conversations = conversationsWithDetails;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Conversation(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "Account");

            if (currentUserId == userId)
                return BadRequest("Kendinize mesaj gönderemezsiniz");

            var otherUser = await _userManager.FindByIdAsync(userId);
            if (otherUser == null)
                return NotFound();

            // Konuşmayı bul veya oluştur
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => (c.User1Id == currentUserId && c.User2Id == userId) ||
                                        (c.User1Id == userId && c.User2Id == currentUserId));

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    User1Id = currentUserId,
                    User2Id = userId,
                    CreatedDate = DateTime.Now,
                    LastMessageDate = DateTime.Now
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            // Mesajları getir
            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                           (m.SenderId == userId && m.ReceiverId == currentUserId))
                .Where(m => !(m.SenderId == currentUserId && m.IsDeletedBySender) &&
                           !(m.ReceiverId == currentUserId && m.IsDeletedByReceiver))
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderBy(m => m.CreatedDate)
                .ToListAsync();

            // Okunmamış mesajları işaretle
            var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList();
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadDate = DateTime.Now;
            }
            _context.Messages.UpdateRange(unreadMessages);
            await _context.SaveChangesAsync();

            ViewBag.OtherUser = otherUser;
            ViewBag.Conversation = conversation;
            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string receiverId, string subject, string content, int? businessId = null, int? reservationId = null)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(senderId))
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            if (senderId == receiverId)
                return Json(new { success = false, message = "Kendinize mesaj gönderemezsiniz" });

            if (string.IsNullOrWhiteSpace(content))
                return Json(new { success = false, message = "Mesaj içeriği boş olamaz" });

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Subject = subject ?? "Mesaj",
                Content = content,
                CreatedDate = DateTime.Now,
                IsRead = false,
                RelatedBusinessId = businessId,
                RelatedReservationId = reservationId
            };

            _context.Messages.Add(message);

            // Konuşmayı güncelle veya oluştur
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => (c.User1Id == senderId && c.User2Id == receiverId) ||
                                        (c.User1Id == receiverId && c.User2Id == senderId));

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    User1Id = senderId,
                    User2Id = receiverId,
                    CreatedDate = DateTime.Now,
                    LastMessageDate = DateTime.Now
                };
                _context.Conversations.Add(conversation);
            }
            else
            {
                conversation.LastMessageDate = DateTime.Now;
                _context.Conversations.Update(conversation);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Mesaj gönderildi", messageId = message.Id });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false });

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == userId);

            if (message != null && !message.IsRead)
            {
                message.IsRead = true;
                message.ReadDate = DateTime.Now;
                _context.Messages.Update(message);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int messageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
                return Json(new { success = false, message = "Mesaj bulunamadı" });

            if (message.SenderId == userId)
                message.IsDeletedBySender = true;
            else if (message.ReceiverId == userId)
                message.IsDeletedByReceiver = true;
            else
                return Json(new { success = false, message = "Bu mesajı silme yetkiniz yok" });

            _context.Messages.Update(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Mesaj silindi" });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { count = 0 });

            var count = await _context.Messages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeletedByReceiver);

            return Json(new { count });
        }
    }
}
