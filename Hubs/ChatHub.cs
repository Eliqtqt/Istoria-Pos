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

            ChatMessage? savedMsg = null;

            try
            {
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
                savedMsg = msg;
            }
            catch
            {
                await Clients.Caller.SendAsync("ChatError", "Database unavailable — message not saved. Please try again.");
                return;
            }

            await Clients.All.SendAsync("ReceiveMessage", new ChatMessageDto
            {
                Id = savedMsg!.Id,
                SenderName = savedMsg.SenderName,
                SenderIsAdmin = savedMsg.SenderIsAdmin,
                MessageText = savedMsg.MessageText,
                SentAt = savedMsg.SentAt.ToString("hh:mm tt"),
                IsRead = savedMsg.IsRead
            });
        }

        public override async Task OnConnectedAsync()
        {
            var messages = new List<ChatMessageDto>();
            try
            {
                var msgs = await _db.ChatMessages
                    .OrderByDescending(m => m.SentAt)
                    .Take(50)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                messages = msgs.Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    SenderName = m.SenderName,
                    SenderIsAdmin = m.SenderIsAdmin,
                    MessageText = m.MessageText,
                    SentAt = m.SentAt.ToString("hh:mm tt"),
                    IsRead = m.IsRead
                }).ToList();
            }
            catch { }

            await Clients.Caller.SendAsync("LoadHistory", messages);
            await base.OnConnectedAsync();
        }
    }
}
