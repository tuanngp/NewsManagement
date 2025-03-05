using System.Security.Claims;
using BusinessObjects.Models;
using FUNewsManagementSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Services;
using Services.ServiceImpl;

namespace FUNewsManagementSystem.Controllers
{
    [Authorize]
    public class NewsController : Controller
    {
        private readonly INewsArticleService _newsArticleService;
        private readonly ICategoryService _categoryService;
        private readonly ISystemAccountService _accountService;
        private readonly ILogger<NewsController> _logger;

        public NewsController(
            INewsArticleService newsArticleService,
            ICategoryService categoryService,
            ISystemAccountService accountService,
            ILogger<NewsController> logger
        )
        {
            _newsArticleService = newsArticleService;
            _categoryService = categoryService;
            _accountService = accountService;
            _logger = logger;
        }

        [Authorize(Roles = "Staff")]
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
            ViewData["TitleSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["CategorySortParm"] = sortOrder == "Category" ? "category_desc" : "Category";
            ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";
            ViewData["ApprovalSortParm"] = sortOrder == "Approval" ? "approval_desc" : "Approval";

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var newsArticles = await _newsArticleService.GetAllNewsByCreatedId(
                short.Parse(userId!)
            );

            if (!String.IsNullOrEmpty(searchString))
            {
                newsArticles = newsArticles
                    .Where(s =>
                        s.NewsTitle.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                        || s.Headline.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                        || s.NewsSource.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                        || (
                            s.Category != null
                            && s.Category.CategoryName.Contains(
                                searchString,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        || (
                            s.CreatedBy != null
                            && s.CreatedBy.AccountName.Contains(
                                searchString,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                    )
                    .ToList();
            }

            newsArticles = sortOrder switch
            {
                "title_desc" => newsArticles.OrderByDescending(s => s.NewsTitle).ToList(),
                "Date" => newsArticles.OrderBy(s => s.CreatedDate).ToList(),
                "date_desc" => newsArticles.OrderByDescending(s => s.CreatedDate).ToList(),
                "Category" => newsArticles
                    .OrderBy(s => s.Category != null ? s.Category.CategoryName : "")
                    .ToList(),
                "category_desc" => newsArticles
                    .OrderByDescending(s => s.Category != null ? s.Category.CategoryName : "")
                    .ToList(),
                "Status" => newsArticles.OrderBy(s => s.Status).ToList(),
                "status_desc" => newsArticles.OrderByDescending(s => s.Status).ToList(),
                "Approval" => newsArticles.OrderBy(s => s.ApprovalStatus).ToList(),
                "approval_desc" => newsArticles.OrderByDescending(s => s.ApprovalStatus).ToList(),
                _ => newsArticles.OrderBy(s => s.NewsTitle).ToList(),
            };

            int pageSize = 5;
            return View(
                await PaginatedList<NewsArticle>.CreateAsync(
                    newsArticles.AsQueryable(),
                    pageNumber ?? 1,
                    pageSize
                )
            );
        }

        [Authorize(Roles = "Lecturer, Admin, Staff")]
        public async Task<IActionResult> Details(string id)
        {
            return await HandleEntityAction(id, _newsArticleService.GetByIdAsync, "Details");
        }

        [Authorize(Roles = "Staff, Admin")]
        public async Task<IActionResult> Preview(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var article = await _newsArticleService.GetByIdAsync(id);
            if (article == null)
                return NotFound();

            if (article.Status != ArticleStatus.Draft)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(article);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Review()
        {
            var pendingArticles = (await _newsArticleService.GetAllAsync())
                .Where(a => a.ApprovalStatus == ApprovalStatus.Pending)
                .OrderByDescending(a => a.CreatedDate)
                .ToList();

            return View(pendingArticles);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id, bool isApproved)
        {
            var article = await _newsArticleService.GetByIdAsync(id);
            if (article == null)
                return NotFound();

            var userId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            article.ApprovalStatus = isApproved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
            article.ApprovedById = userId;
            article.ApprovedDate = DateTime.Now;

            if (isApproved && article.PublishDate <= DateTime.Now)
            {
                article.Status = ArticleStatus.Published;
            }

            await _newsArticleService.UpdateAsync(article);
            return RedirectToAction(nameof(Review));
        }

        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Create()
        {
            await PrepareDropdownLists();
            return View();
        }

        [Authorize(Roles = "Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsArticle newsArticle, bool saveAsDraft)
        {
            newsArticle.CreatedDate = DateTime.Now;
            newsArticle.ModifiedDate = DateTime.Now;
            newsArticle.NewsArticleId = Guid.NewGuid().ToString().Substring(0, 19);
            var userId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            newsArticle.CreatedById = userId;
            newsArticle.UpdatedById = userId;

            // Xử lý trạng thái bài viết
            newsArticle.Status = saveAsDraft ? ArticleStatus.Draft : ArticleStatus.Published;

            // Nếu không phải draft, kiểm tra ngày xuất bản
            if (!saveAsDraft)
            {
                if (newsArticle.PublishDate == null || newsArticle.PublishDate <= DateTime.Now)
                {
                    newsArticle.Status = ArticleStatus.Published;
                    newsArticle.PublishDate = DateTime.Now;
                }
                else
                {
                    newsArticle.Status = ArticleStatus.Draft;
                }
            }
            return await HandleCreateUpdate(
                newsArticle,
                _newsArticleService.AddAsync,
                nameof(Index)
            );
        }

        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Edit(string id)
        {
            await PrepareDropdownLists();
            return await HandleEntityAction(id, _newsArticleService.GetByIdAsync, "Edit");
        }

        [Authorize(Roles = "Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(NewsArticle newsArticle, bool saveAsDraft)
        {
            var userId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            newsArticle.ModifiedDate = DateTime.Now;
            newsArticle.UpdatedById = userId;

            // Xử lý trạng thái bài viết
            if (saveAsDraft)
            {
                newsArticle.Status = ArticleStatus.Draft;
            }
            else
            {
                if (newsArticle.PublishDate == null || newsArticle.PublishDate <= DateTime.Now)
                {
                    newsArticle.Status = ArticleStatus.Published;
                    newsArticle.PublishDate = DateTime.Now;
                }
                else
                {
                    newsArticle.Status = ArticleStatus.Draft;
                }
            }

            // Reset approval status khi có chỉnh sửa
            newsArticle.ApprovalStatus = ApprovalStatus.Pending;
            newsArticle.ApprovedById = null;
            newsArticle.ApprovedDate = null;

            if (!ModelState.IsValid)
            {
                await PrepareDropdownLists();
                return View(newsArticle);
            }
            return await HandleCreateUpdate(
                newsArticle,
                _newsArticleService.UpdateAsync,
                nameof(Index)
            );
        }

        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Delete(string id)
        {
            return await HandleEntityAction(id, _newsArticleService.GetByIdAsync, "Delete");
        }

        [Authorize(Roles = "Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(NewsArticle newsArticle, IFormCollection _)
        {
            if (newsArticle == null)
            {
                return NotFound();
            }

            var id = newsArticle.NewsArticleId;

            try
            {
                await _newsArticleService.DeleteAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể xóa bài viết vì nó đang được sử dụng trong hệ thống."
                );
                return View(newsArticle);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xóa. Vui lòng thử lại.");
                return View(newsArticle);
            }
        }

        private async Task PrepareDropdownLists()
        {
            var categories = await _categoryService.GetAllAsync();
            if (categories == null || !categories.Any())
            {
                _logger.LogWarning("No categories found in the database.");
                ViewBag.CategoryId = new SelectList(
                    new List<SelectListItem>
                    {
                        new SelectListItem { Text = "Không có danh mục", Value = "" },
                    },
                    "Value",
                    "Text"
                );
            }
            else
            {
                var categoryItems = new List<SelectListItem>();
                var parentCategories = categories.Where(c => c.ParentCategoryId == null).ToList();

                foreach (var parent in parentCategories)
                {
                    categoryItems.Add(
                        new SelectListItem
                        {
                            Text = $"{parent.CategoryName}",
                            Value = parent.CategoryId.ToString(),
                        }
                    );

                    var subCategories = categories
                        .Where(c => c.ParentCategoryId == parent.CategoryId)
                        .ToList();
                    foreach (var sub in subCategories)
                    {
                        categoryItems.Add(
                            new SelectListItem
                            {
                                Text = $"── {sub.CategoryName}",
                                Value = sub.CategoryId.ToString(),
                            }
                        );
                    }
                }

                if (!parentCategories.Any())
                {
                    categoryItems.AddRange(
                        categories.Select(c => new SelectListItem
                        {
                            Text = c.CategoryName,
                            Value = c.CategoryId.ToString(),
                        })
                    );
                }

                ViewBag.CategoryId = new SelectList(categoryItems, "Value", "Text");
            }

            var accounts = await _accountService.GetAllAsync();
            ViewBag.CreatedById = new SelectList(accounts, "AccountId", "AccountName");
            ViewBag.UpdatedById = new SelectList(accounts, "AccountId", "AccountName");
        }

        private async Task<IActionResult> HandleEntityAction<T>(
            string id,
            Func<string, Task<T?>> getFunc,
            string viewName
        )
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();
            var entity = await getFunc(id);
            if (entity == null)
                return NotFound();
            return View(viewName, entity);
        }

        private async Task<IActionResult> HandleCreateUpdate<T>(
            T entity,
            Func<T, Task> action,
            string redirectAction
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
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xử lý yêu cầu của bạn.");
                await PrepareDropdownLists();
                return View(entity);
            }
        }
    }
}
