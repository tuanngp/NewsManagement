using System.Security.Claims;
using BusinessObjects.Models;
using FUNewsManagementSystem.Helpers;
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

        public async Task<IActionResult> Index(
            string searchString,
            string sortOrder,
            int? pageNumber,
            string currentFilter
        )
        {
            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["EmailSortParm"] = sortOrder == "Email" ? "email_desc" : "Email";
            ViewData["RoleSortParm"] = sortOrder == "Role" ? "role_desc" : "Role";

            var systemAccounts = await _systemAccountService.GetAllAsync();

            if (!String.IsNullOrEmpty(searchString))
            {
                systemAccounts = systemAccounts
                    .Where(s =>
                        s.AccountName.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                        || s.AccountEmail.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();
            }

            systemAccounts = sortOrder switch
            {
                "name_desc" => systemAccounts.OrderByDescending(s => s.AccountName).ToList(),
                "Email" => systemAccounts.OrderBy(s => s.AccountEmail).ToList(),
                "email_desc" => systemAccounts.OrderByDescending(s => s.AccountEmail).ToList(),
                "Role" => systemAccounts.OrderBy(s => s.AccountRole).ToList(),
                "role_desc" => systemAccounts.OrderByDescending(s => s.AccountRole).ToList(),
                _ => systemAccounts.OrderBy(s => s.AccountName).ToList(),
            };

            int pageSize = 10;
            return View(
                await PaginatedList<SystemAccount>.CreateAsync(
                    systemAccounts.AsQueryable(),
                    pageNumber ?? 1,
                    pageSize
                )
            );
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
            var existingAccount = await _systemAccountService.GetByIdAsync(systemAccount.AccountId);
            if (existingAccount == null)
            {
                return NotFound();
            }

            existingAccount.AccountName = systemAccount.AccountName;
            existingAccount.AccountEmail = systemAccount.AccountEmail;
            existingAccount.AccountRole = systemAccount.AccountRole;
            if (!string.IsNullOrEmpty(systemAccount.AccountPassword))
            {
                existingAccount.AccountPassword = systemAccount.AccountPassword;
            }

            return await HandleCreateUpdate(
                existingAccount,
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

        [Authorize]
        public async Task<IActionResult> Profile()
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

            return View(account);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            [Bind("AccountId,AccountName,AccountEmail,AccountPassword")] SystemAccount systemAccount
        )
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !short.TryParse(userId, out short id))
            {
                return NotFound();
            }

            if (id != systemAccount.AccountId)
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

                // Preserve the existing role
                existingAccount.AccountName = systemAccount.AccountName;
                if (!string.IsNullOrEmpty(systemAccount.AccountPassword))
                {
                    existingAccount.AccountPassword = systemAccount.AccountPassword;
                }

                await _systemAccountService.UpdateAsync(existingAccount);

                // Update the authentication cookie with new user details
                await SignInAsync(existingAccount);

                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", id);
                ModelState.AddModelError("", "An error occurred while updating your profile.");
                return View("Profile", systemAccount);
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
                string.IsNullOrEmpty(systemAccount.AccountEmail)
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
                var admin = _systemAccountService.Login(email!, password!);
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
