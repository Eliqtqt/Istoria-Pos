using CafeWebsite.Data;
using CafeWebsite.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CafeWebsite.Hubs
{
    public class ChatHub : Hub
    {
        private readonly CafeDbContext _db;

        public ChatHub(CafeDbContext db)
        {
            _db = db;
        }

        public async Task SendMessage(string senderName, bool senderIsAdmin, string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
                return;

            var msg = new ChatMessage
            {
                SenderName = senderName?.Trim() ?? "Visitor",
                SenderIsAdmin = senderIsAdmin,
                MessageText = messageText.Trim(),
                SentAt = DateTime.Now,
                IsRead = false
            };

            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            await Clients.All.SendAsync("ReceiveMessage", new
            {
                id = msg.Id,
                senderName = msg.SenderName,
                senderIsAdmin = msg.SenderIsAdmin,
                messageText = msg.MessageText,
                sentAt = msg.SentAt.ToString("hh:mm tt"),
                isRead = msg.IsRead
            });
        }

        public override async Task OnConnectedAsync()
        {
            var recent = await _db.ChatMessages
                .OrderByDescending(m => m.SentAt)
                .Take(50)
                .OrderBy(m => m.SentAt)
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

            await Clients.Caller.SendAsync("LoadHistory", recent);
            await base.OnConnectedAsync();
        }
    }
}
