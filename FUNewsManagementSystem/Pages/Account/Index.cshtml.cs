using BusinessObjects.Models;
using FUNewsManagementSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services;

namespace FUNewsManagementSystem.Pages.Account
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ISystemAccountService _systemAccountService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ISystemAccountService systemAccountService,
            ILogger<IndexModel> logger)
        {
            _systemAccountService = systemAccountService ?? throw new ArgumentNullException(nameof(systemAccountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string NameSort { get; set; } = string.Empty;
        public string EmailSort { get; set; } = string.Empty;
        public string RoleSort { get; set; } = string.Empty;
        public string CurrentFilter { get; set; } = string.Empty;
        public string CurrentSort { get; set; } = string.Empty;

        public PaginatedList<SystemAccount> SystemAccounts { get; set; } = default!;

        public async Task OnGetAsync(string sortOrder, string searchString, string currentFilter, int? pageIndex)
        {
            CurrentSort = sortOrder;
            NameSort = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            EmailSort = sortOrder == "Email" ? "email_desc" : "Email";
            RoleSort = sortOrder == "Role" ? "role_desc" : "Role";

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            CurrentFilter = searchString;

            var systemAccounts = await _systemAccountService.GetAllAsync();

            if (!string.IsNullOrEmpty(searchString))
            {
                systemAccounts = systemAccounts
                    .Where(s =>
                        s.AccountName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true ||
                        s.AccountEmail?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true
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
            SystemAccounts = await PaginatedList<SystemAccount>.CreateAsync(
                systemAccounts.AsQueryable(),
                pageIndex ?? 1,
                pageSize
            );
        }
    }
} 