using System.Security.Claims;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services;

namespace FUNewsManagementSystem.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly ISystemAccountService _systemAccountService;
        private readonly ILogger<ProfileModel> _logger;

        public ProfileModel(
            ISystemAccountService systemAccountService,
            ILogger<ProfileModel> logger)
        {
            _systemAccountService = systemAccountService ?? throw new ArgumentNullException(nameof(systemAccountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty]
        public SystemAccount Account { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !short.TryParse(userId, out short id))
            {
                return NotFound();
            }

            var account = await _systemAccountService.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            Account = account;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !short.TryParse(userId, out short id))
            {
                return NotFound();
            }

            if (id != Account.AccountId)
            {
                return NotFound();
            }

            try
            {
                var existingAccount = await _systemAccountService.GetByIdAsync(id);
                if (existingAccount == null)
                {
                    return NotFound();
                }

                // Preserve the existing role and email
                existingAccount.AccountName = Account.AccountName;
                if (!string.IsNullOrEmpty(Account.AccountPassword))
                {
                    existingAccount.AccountPassword = Account.AccountPassword;
                }

                await _systemAccountService.UpdateAsync(existingAccount);

                // Update the authentication cookie with new user details
                await SignInAsync(existingAccount);

                TempData["SuccessMessage"] = "Hồ sơ đã được cập nhật thành công.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", id);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật hồ sơ của bạn.");
                return Page();
            }
        }

        private async Task SignInAsync(SystemAccount account)
        {
            var role = account.AccountRole switch
            {
                0 => "Admin",
                1 => "Staff",
                _ => "Lecturer",
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, account.AccountName ?? "Unknown"),
                new Claim(ClaimTypes.Email, account.AccountEmail ?? ""),
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
                }
            );
        }
    }
} 