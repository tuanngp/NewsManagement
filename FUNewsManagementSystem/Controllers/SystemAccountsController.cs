using System.Security.Claims;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Services;

namespace FUNewsManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SystemAccountsController : Controller
    {
        private readonly ISystemAccountService _systemAccountService;
        private readonly ILogger<SystemAccountsController> _logger;
        private readonly IConfiguration _configuration;

        public SystemAccountsController(
            ISystemAccountService systemAccountService,
            ILogger<SystemAccountsController> logger,
            IConfiguration configuration
        )
        {
            _systemAccountService =
                systemAccountService
                ?? throw new ArgumentNullException(nameof(systemAccountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration =
                configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<IActionResult> Index()
        {
            var systemAccounts = await _systemAccountService.GetAllAsync();
            return View(systemAccounts);
        }

        public async Task<IActionResult> Details(short? id)
        {
            return await HandleEntityAction(id, _systemAccountService.GetByIdAsync, "Details");
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("AccountId,AccountName,AccountEmail,AccountRole,AccountPassword")]
                SystemAccount systemAccount
        )
        {
            return await HandleCreateUpdate(
                systemAccount,
                _systemAccountService.AddAsync,
                nameof(Index)
            );
        }

        public async Task<IActionResult> Edit(short? id)
        {
            return await HandleEntityAction(id, _systemAccountService.GetByIdAsync, "Edit");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [Bind("AccountId,AccountName,AccountEmail,AccountRole,AccountPassword")]
                SystemAccount systemAccount
        )
        {
            return await HandleCreateUpdate(
                systemAccount,
                _systemAccountService.UpdateAsync,
                nameof(Index)
            );
        }

        public async Task<IActionResult> Delete(short? id)
        {
            return await HandleEntityAction(id, _systemAccountService.GetByIdAsync, "Delete");
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(short id)
        {
            try
            {
                var systemAccount = await _systemAccountService.GetByIdAsync(id);
                if (systemAccount == null)
                {
                    return NotFound();
                }

                await _systemAccountService.DeleteAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể xóa tài khoản vì nó đang được sử dụng trong hệ thống."
                );
                return View();
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xóa. Vui lòng thử lại.");
                return View();
            }
        }

        private async Task<IActionResult> HandleEntityAction<T>(
            short? id,
            Func<short, Task<T?>> getFunc,
            string viewName
        )
        {
            if (!id.HasValue)
                return NotFound();
            var entity = await getFunc(id.Value);
            if (entity == null)
                return NotFound();
            return View(viewName, entity);
        }

        private async Task<IActionResult> HandleCreateUpdate<T>(
            T entity,
            Func<T, Task> action,
            string redirectAction,
            Func<Exception, IActionResult>? customExceptionHandler = null
        )
        {
            if (!ModelState.IsValid)
                return View(entity);
            try
            {
                await action(entity);
                return RedirectToAction(redirectAction);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error performing CRUD operation on {EntityType}",
                    typeof(T).Name
                );
                if (customExceptionHandler != null)
                {
                    var result = customExceptionHandler(ex);
                    if (result != null)
                        return result;
                }
                ModelState.AddModelError("", "An error occurred while processing your request.");
                return View(entity);
            }
        }

        private async Task<bool> SystemAccountExists(short id)
        {
            return await _systemAccountService.GetByIdAsync(id) != null;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = "/Home/Index")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            SystemAccount systemAccount,
            string returnUrl = "/Home/Index"
        )
        {
            if (
                !ModelState.IsValid
                || string.IsNullOrEmpty(systemAccount.AccountEmail)
                || string.IsNullOrEmpty(systemAccount.AccountPassword)
            )
            {
                ViewBag.Error = "Please provide email and password.";
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            var adminEmail = _configuration["AdminAccount:Email"];
            var adminPassword = _configuration["AdminAccount:Password"];
            var email = systemAccount.AccountEmail;
            var password = systemAccount.AccountPassword;

            if (email == adminEmail && password == adminPassword)
            {
                var admin = new SystemAccount
                {
                    AccountRole = 0,
                    AccountName = "Administrator",
                    AccountEmail = adminEmail,
                };
                await SignInAsync(admin);
                return RedirectToValidUrl(returnUrl);
            }

            var account = _systemAccountService.Login(email!, password!);
            if (account != null)
            {
                await SignInAsync(account);
                return RedirectToValidUrl(returnUrl);
            }

            ViewBag.Error = "Invalid credentials";
            ViewData["ReturnUrl"] = returnUrl;
            return View(systemAccount);
        }

        [AllowAnonymous]
        public IActionResult GoogleLogin(string returnUrl = "/Home/Index")
        {
            var redirectUrl = Url.Action(
                nameof(GoogleResponse),
                "SystemAccounts",
                new { returnUrl }
            );
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse(string returnUrl = "/Home/Index")
        {
            var info = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (info == null || !info.Succeeded)
            {
                return RedirectToAction(
                    "Login",
                    new { returnUrl, error = "Đăng nhập Google thất bại." }
                );
            }

            var email = info.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = info.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var googleId = info.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var account = _systemAccountService.FindByEmail(email!);
            if (account == null)
            {
                var id = _systemAccountService.GetAllAsync().Result.Count() + 1;
                account = new SystemAccount
                {
                    AccountId = (short)id,
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

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "SystemAccounts");
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
            if (Url.IsLocalUrl(returnUrl) && !returnUrl.Contains("/SystemAccounts/Login"))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
