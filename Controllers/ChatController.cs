using System.Collections.Generic;
using CafeWebsite.Data;
using CafeWebsite.Models;
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
            if (TempData["ChatError"] != null)
                ViewBag.ChatError = TempData["ChatError"];
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

            try
            {
                var msg = new ChatMessage
                {
                    SenderName = senderName,
                    SenderIsAdmin = senderIsAdmin,
                    MessageText = message.Trim(),
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _db.ChatMessages.Add(msg);
                await _db.SaveChangesAsync();
            }
            catch
            {
                TempData["ChatError"] = "Database unavailable — your message was not saved. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            try
            {
                var messages = await _db.ChatMessages
                    .OrderBy(m => m.SentAt)
                    .Take(100)
                    .Select(m => new ChatMessageDto
                    {
                        Id = m.Id,
                        SenderName = m.SenderName,
                        SenderIsAdmin = m.SenderIsAdmin,
                        MessageText = m.MessageText,
                        SentAt = m.SentAt.ToString("hh:mm tt"),
                        IsRead = m.IsRead
                    })
                    .ToListAsync();

                return Json(messages);
            }
            catch
            {
                return Json(new List<ChatMessageDto>());
            }
        }
    }
}
