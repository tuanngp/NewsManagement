using System.Security.Claims;
using BusinessObjects.Models;
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
        public async Task<IActionResult> Index()
        {
            var newsArticles = await _newsArticleService.GetAllAsync();
            return View(newsArticles);
        }

        [Authorize(Roles = "Lecturer, Admin, Staff")]
        public async Task<IActionResult> Details(string id)
        {
            return await HandleEntityAction(id, _newsArticleService.GetByIdAsync, "Details");
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
        public async Task<IActionResult> Create(NewsArticle newsArticle)
        {
            newsArticle.CreatedDate = DateTime.Now;
            newsArticle.ModifiedDate = DateTime.Now;
            newsArticle.NewsArticleId = Guid.NewGuid().ToString().Substring(0, 20);
            var userId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            newsArticle.CreatedById = userId;
            newsArticle.UpdatedById = userId;

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                ModelState.AddModelError("", "Error: " + string.Join("; ", errors));

                await PrepareDropdownLists();
                return View(newsArticle);
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
        public async Task<IActionResult> Edit(NewsArticle newsArticle)
        {
            var userId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            newsArticle.ModifiedDate = DateTime.Now;
            newsArticle.UpdatedById = userId;

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
                    // Thêm danh mục cha với Value là CategoryId, không Disabled
                    categoryItems.Add(
                        new SelectListItem
                        {
                            Text = $"{parent.CategoryName}",
                            Value = parent.CategoryId.ToString(),
                        }
                    );

                    // Thêm danh mục con với thụt đầu dòng
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

                // Trường hợp không có danh mục cha
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
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xử lý yêu cầu của bạn.");
                return View(entity);
            }
        }
    }
}
