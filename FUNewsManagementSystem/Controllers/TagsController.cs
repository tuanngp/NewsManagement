﻿using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services;

namespace FUNewsManagementSystem.Controllers
{
    [Authorize(Roles = "Staff")]
    public class TagsController : Controller
    {
        private readonly ITagService _tagService;
        private readonly ILogger<TagsController> _logger;

        public TagsController(ITagService tagService, ILogger<TagsController> logger)
        {
            _tagService = tagService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString, string currentFilter, int? pageNumber)
        {
            ViewData["CurrentSort"] = "";
            
            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var tags = await _tagService.GetAllAsync();

            if (!String.IsNullOrEmpty(searchString))
            {
                tags = tags.Where(t => t.TagName.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                                  || t.Note.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                          .ToList();
            }

            int pageSize = 10;
            return View(await Helpers.PaginatedList<Tag>.CreateAsync(tags.AsQueryable(), pageNumber ?? 1, pageSize));
        }

        public async Task<IActionResult> Details(int? id)
        {
            return await HandleEntityAction(id, _tagService.GetByIdAsync, "Details");
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TagId,TagName,Note")] Tag tag)
        {
            return await HandleCreateUpdate(tag, _tagService.AddAsync, nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            return await HandleEntityAction(id, _tagService.GetByIdAsync, "Edit");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TagId,TagName,Note")] Tag tag)
        {
            if (id != tag.TagId)
                return NotFound();
            return await HandleCreateUpdate(tag, _tagService.UpdateAsync, nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            return await HandleEntityAction(id, _tagService.GetByIdAsync, "Delete");
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Tag tag)
        {
            var id = tag.TagId;
            try
            {
                await _tagService.DeleteAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "Lỗi khi xóa thẻ. Hãy đảm bảo không có dữ liệu tham chiếu."
                );
                return View(tag);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi: " + ex.Message);
                return View(tag);
            }
        }

        private async Task<IActionResult> HandleEntityAction<T>(
            int? id,
            Func<int, Task<T?>> getFunc,
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
                ModelState.AddModelError("", "An error occurred while processing your request.");
                return View(entity);
            }
        }
    }
}
