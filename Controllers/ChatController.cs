using CafeWebsite.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeWebsite.Controllers
{
    public class ChatController : Controller
    {
        private readonly CafeDbContext _db;
        public ChatController(CafeDbContext db) => _db = db;

        public IActionResult Index()
        {
            ViewData["Title"] = "Live Chat";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromForm] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return RedirectToAction(nameof(Index));

            var senderName = HttpContext.Session.GetString("ChatVisitorName") ?? "Visitor";
            var senderIsAdmin = User?.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin");

            var msg = new Models.ChatMessage
            {
                SenderName = senderName,
                SenderIsAdmin = senderIsAdmin,
                MessageText = message.Trim(),
                SentAt = DateTime.Now,
                IsRead = false
            };

            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var messages = await _db.ChatMessages
                .OrderBy(m => m.SentAt)
                .Take(100)
                .Select(m => new
                {
                    id = m.Id,
                    senderName = m.SenderName,
                    senderIsAdmin = m.SenderIsAdmin,
                    messageText = m.MessageText,
                    sentAt = m.SentAt.ToString("hh:mm tt"),
                    isRead = m.IsRead
                })
                .ToListAsync();

            return Json(messages);
        }
    }
}
