using BusinessObjects.Models;
using FUNewsManagementSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services;

namespace FUNewsManagementSystem.Controllers
{
    [Authorize(Roles = "Staff")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            ICategoryService categoryService,
            ILogger<CategoriesController> logger
        )
        {
            _categoryService =
                categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index(string searchString, string sortOrder, int? pageNumber, string currentFilter)
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
            ViewData["DescriptionSortParm"] = sortOrder == "Description" ? "description_desc" : "Description";
            ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";

            var categories = await _categoryService.GetAllAsync();

            if (!String.IsNullOrEmpty(searchString))
            {
                categories = categories.Where(s => s.CategoryName.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                                             || (s.CategoryDesciption != null && s.CategoryDesciption.Contains(searchString, StringComparison.OrdinalIgnoreCase)))
                                     .ToList();
            }

            categories = sortOrder switch
            {
                "name_desc" => categories.OrderByDescending(s => s.CategoryName).ToList(),
                "Description" => categories.OrderBy(s => s.CategoryDesciption).ToList(),
                "description_desc" => categories.OrderByDescending(s => s.CategoryDesciption).ToList(),
                "Status" => categories.OrderBy(s => s.IsActive).ToList(),
                "status_desc" => categories.OrderByDescending(s => s.IsActive).ToList(),
                _ => categories.OrderBy(s => s.CategoryName).ToList(),
            };

            int pageSize = 10;
            return View(await PaginatedList<Category>.CreateAsync(categories.AsQueryable(), pageNumber ?? 1, pageSize));
        }

        public async Task<IActionResult> Details(short? id)
        {
            return await HandleEntityAction(id, _categoryService.GetByIdAsync, "Details");
        }

        public async Task<IActionResult> Create()
        {
            await PopulateParentCategoryDropdown();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CategoryId,CategoryName,CategoryDesciption,ParentCategoryId,IsActive")]
                Category category
        )
        {
            if (!ModelState.IsValid)
            {
                await PopulateParentCategoryDropdown(category.ParentCategoryId);
                return View(category);
            }
            await _categoryService.AddAsync(category);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(short? id)
        {
            var result = await HandleEntityAction(id, _categoryService.GetByIdAsync, "Edit");
            if (result is ViewResult viewResult)
            {
                await PopulateParentCategoryDropdown(
                    ((Category)viewResult.Model!).ParentCategoryId
                );
            }
            return result;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [Bind("CategoryId,CategoryName,CategoryDesciption,ParentCategoryId,IsActive")]
                Category category
        )
        {
            if (!ModelState.IsValid)
            {
                await PopulateParentCategoryDropdown(category.ParentCategoryId);
                return View(category);
            }
            try
            {
                await _categoryService.UpdateAsync(category);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category with id {Id}", category.CategoryId);
                if (
                    ex is DbUpdateConcurrencyException
                    && !await CategoryExists(category.CategoryId)
                )
                    return NotFound();
                ModelState.AddModelError("", "An error occurred while updating the category.");
                await PopulateParentCategoryDropdown(category.ParentCategoryId);
                return View(category);
            }
        }

        public async Task<IActionResult> Delete(short? id)
        {
            return await HandleEntityAction(id, _categoryService.GetByIdAsync, "Delete");
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Category category)
        {
            if (category == null)
            {
                return NotFound();
            }
            try
            {
                await _categoryService.DeleteAsync(category.CategoryId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không thể xóa vì danh mục này đang được sử dụng.");
                return View(category);
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

        private async Task PopulateParentCategoryDropdown(short? selectedId = null)
        {
            var categories = await _categoryService.GetAllAsync();
            categories = categories.Where(c => c.CategoryId != c.ParentCategoryId).ToList();
            ViewData["ParentCategoryId"] = new SelectList(
                categories,
                "CategoryId",
                "CategoryName",
                selectedId
            );
        }

        private async Task<bool> CategoryExists(short id)
        {
            return await _categoryService.GetByIdAsync(id) != null;
        }
    }
}
