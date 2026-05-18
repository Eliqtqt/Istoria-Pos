using CafeWebsite.Data;
using CafeWebsite.Models;
using CafeWebsite.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;

namespace CafeWebsite.Controllers
{
    public class AccountController : Controller
    {
        private readonly CafeDbContext _context;
        private readonly IEmailSender _emailSender;

        public AccountController(CafeDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            TempData["Username"] = username;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View();
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Please confirm your email address before logging in. Check your inbox for the verification link or request a new one.");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmation(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Please enter your email address.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                TempData["Error"] = "If an account exists with that email, a confirmation link has been sent.";
                return RedirectToAction("Login");
            }

            if (user.EmailConfirmed)
            {
                TempData["Error"] = "This email is already confirmed. You can log in.";
                return RedirectToAction("Login");
            }

            // Generate new token
            user.EmailConfirmationToken = Guid.NewGuid().ToString();
            user.TokenExpiration = DateTime.UtcNow.AddHours(24);
            await _context.SaveChangesAsync();

            var confirmationLink = Url.Action("VerifyEmail", "Account", new { email = email, token = user.EmailConfirmationToken }, Request.Scheme);
            
            try
            {
                await _emailSender.SendEmailConfirmationAsync(email, user.Username, confirmationLink!);
                TempData["Success"] = "A new confirmation email has been sent. Please check your inbox.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                TempData["Error"] = "Failed to send confirmation email. Please try again later.";
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match";
                TempData["RegUsername"] = username;
                TempData["RegEmail"] = email;
                return View();
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username || u.Email == email);
            if (existingUser != null)
            {
                TempData["Error"] = "Username or email already exists";
                TempData["RegUsername"] = username;
                TempData["RegEmail"] = email;
                return View();
            }

            if (password.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters long";
                TempData["RegUsername"] = username;
                TempData["RegEmail"] = email;
                return View();
            }

            var token = Guid.NewGuid().ToString();
            var tokenExpiration = DateTime.UtcNow.AddHours(24);

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                Role = "Customer",
                EmailConfirmed = false,
                EmailConfirmationToken = token,
                TokenExpiration = tokenExpiration
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send confirmation email
            var confirmationLink = Url.Action("VerifyEmail", "Account", new { email = email, token = token }, Request.Scheme);
            try
            {
                await _emailSender.SendEmailConfirmationAsync(email, username, confirmationLink!);
                TempData["Success"] = "Registration successful! Please check your email to confirm your account before logging in.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                TempData["Error"] = "Registration successful but we couldn't send the confirmation email. Please contact support.";
            }

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid verification link.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.EmailConfirmationToken == token);

            if (user == null)
            {
                TempData["Error"] = "Invalid or expired verification link.";
                return RedirectToAction("Login");
            }

            if (user.TokenExpiration < DateTime.UtcNow)
            {
                TempData["Error"] = "This verification link has expired. Please request a new one.";
                return RedirectToAction("Login");
            }

            user.EmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.TokenExpiration = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Email confirmed successfully! You can now log in.";
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
