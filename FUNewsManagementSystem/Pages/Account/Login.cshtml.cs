using System.ComponentModel.DataAnnotations;
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
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly ISystemAccountService _systemAccountService;
        private readonly ILogger<LoginModel> _logger;
        private readonly IConfiguration _configuration;

        public LoginModel(
            ISystemAccountService systemAccountService,
            ILogger<LoginModel> logger,
            IConfiguration configuration
        )
        {
            _systemAccountService = systemAccountService ?? throw new ArgumentNullException(nameof(systemAccountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }

        public void OnGet(string returnUrl = "/")
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = "/")
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var adminEmail = _configuration["AdminAccount:Email"];
            var adminPassword = _configuration["AdminAccount:Password"];
            var email = Input.Email;
            var password = Input.Password;

            if (email == adminEmail && password == adminPassword)
            {
                var admin = _systemAccountService.Login(email, password);
                if (admin == null)
                {
                    admin = new SystemAccount
                    {
                        AccountId = 0,
                        AccountRole = 0,
                        AccountName = "Administrator",
                        AccountEmail = adminEmail,
                        AccountPassword = adminPassword,
                    };
                    await _systemAccountService.AddAsync(admin);
                }

                await SignInAsync(admin);
                return RedirectToValidUrl(returnUrl);
            }

            var account = _systemAccountService.Login(email, password);
            if (account != null)
            {
                await SignInAsync(account);
                return RedirectToValidUrl(returnUrl);
            }

            ErrorMessage = "Invalid credentials";
            return Page();
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