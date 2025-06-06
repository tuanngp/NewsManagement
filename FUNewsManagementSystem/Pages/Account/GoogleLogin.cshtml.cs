using System.Security.Claims;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services;

namespace FUNewsManagementSystem.Pages.Account
{
    [AllowAnonymous]
    public class GoogleLoginModel : PageModel
    {
        private readonly ISystemAccountService _systemAccountService;
        private readonly ILogger<GoogleLoginModel> _logger;

        public GoogleLoginModel(
            ISystemAccountService systemAccountService,
            ILogger<GoogleLoginModel> logger)
        {
            _systemAccountService = systemAccountService ?? throw new ArgumentNullException(nameof(systemAccountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult OnGet(string returnUrl = "/")
        {
            var redirectUrl = Url.Page("./GoogleResponse", pageHandler: null, values: new { returnUrl }, protocol: Request.Scheme);
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
    }
}

namespace FUNewsManagementSystem.Pages.Account
{
    [AllowAnonymous]
    public class GoogleResponseModel : PageModel
    {
        private readonly ISystemAccountService _systemAccountService;
        private readonly ILogger<GoogleResponseModel> _logger;

        public GoogleResponseModel(
            ISystemAccountService systemAccountService,
            ILogger<GoogleResponseModel> logger)
        {
            _systemAccountService = systemAccountService ?? throw new ArgumentNullException(nameof(systemAccountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = "/")
        {
            var info = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (info == null || !info.Succeeded)
            {
                return RedirectToPage("./Login", new { returnUrl, error = "Đăng nhập Google thất bại." });
            }

            var email = info.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = info.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var googleId = info.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var account = _systemAccountService.FindByEmail(email!);
            if (account == null)
            {
                account = new SystemAccount
                {
                    AccountEmail = email,
                    AccountName = name,
                    AccountRole = 2,
                    AccountPassword = "123",
                };
                await _systemAccountService.AddAsync(account);
            }

            await SignInAsync(account);
            return RedirectToValidUrl(returnUrl);
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

        private IActionResult RedirectToValidUrl(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl) && !returnUrl.Contains("/Account/Login"))
            {
                return Redirect(returnUrl);
            }
            return RedirectToPage("/Index");
        }
    }
} 