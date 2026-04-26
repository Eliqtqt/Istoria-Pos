using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace CafeWebsite.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string htmlMessage);
        Task SendEmailConfirmationAsync(string to, string username, string confirmationLink);
    }

    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            var smtpHost = _configuration["SmtpSettings:Host"];
            var smtpPort = int.Parse(_configuration["SmtpSettings:Port"] ?? "587");
            var smtpUsername = _configuration["SmtpSettings:Username"];
            var smtpPassword = _configuration["SmtpSettings:Password"];
            var fromEmail = _configuration["SmtpSettings:FromEmail"] ?? smtpUsername;

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                // Log warning - email not configured
                Console.WriteLine("[EmailSender] SMTP settings not configured. Email sending skipped.");
                return;
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "Istoria Coffee"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            try
            {
                await client.SendMailAsync(mailMessage);
                Console.WriteLine($"[EmailSender] Verification email sent to {to}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailSender] Error sending email: {ex.Message}");
                throw;
            }
        }

        public async Task SendEmailConfirmationAsync(string to, string username, string confirmationLink)
        {
            var subject = "Confirm your Istoria Coffee account";
            var htmlMessage = $@<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background: #f8f8f8;
        }}
        .container {{
            background: #ffffff;
            padding: 40px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .logo {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .logo h1 {{
            font-family: 'Playfair Display', Georgia, serif;
            font-size: 28px;
            color: #8B4513;
            margin: 0;
        }}
        h2 {{
            color: #333;
            font-size: 20px;
            margin-bottom: 20px;
        }}
        p {{
            margin-bottom: 15px;
            color: #666;
        }}
        .button {{
            display: inline-block;
            padding: 12px 30px;
            background: #8B4513;
            color: #ffffff;
            text-decoration: none;
            border-radius: 4px;
            font-weight: 600;
            margin: 20px 0;
        }}
        .button:hover {{
            background: #A0522D;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #eee;
            font-size: 12px;
            color: #999;
            text-align: center;
        }}
        .note {{
            background: #f9f9f9;
            padding: 15px;
            border-left: 3px solid #8B4513;
            margin: 20px 0;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""logo"">
            <h1>ISTORIA Coffee</h1>
        </div>
        
        <h2>Welcome, {username}!</h2>
        
        <p>Thank you for creating an account with us. To complete your registration and start enjoying our menu, please confirm your email address by clicking the button below:</p>
        
        <p style=""text-align: center;"">
            <a href=""{confirmationLink}"" class=""button"">Confirm Email Address</a>
        </p>
        
        <div class=""note"">
            <strong>Note:</strong> This link will expire in 24 hours for security purposes. If you didn't create an account, you can safely ignore this email.
        </div>
        
        <p>If the button above doesn't work, you can also copy and paste this link into your browser:</p>
        <p style=""word-break: break-all; color: #8B4513;"">{confirmationLink}</p>
        
        <div class=""footer"">
            <p>&copy; 2024 Istoria Coffee. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(to, subject, htmlMessage);
        }
    }
}
