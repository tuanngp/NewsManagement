using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Services;
using Services.ServiceImpl;

namespace FUNewsManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly INewsArticleService _newsArticleService;
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;

        public HomeController(
            INewsArticleService newsArticleService,
            ICategoryService categoryService,
            ITagService tagService
        )
        {
            _newsArticleService = newsArticleService;
            _categoryService = categoryService;
            _tagService = tagService;
        }

        public async Task<IActionResult> IndexAsync()
        {
            var newsArticles = await _newsArticleService.GetAllActiveNews();

            var categories = await _categoryService.GetAllAsync();
            var categoryViewModels = categories.Select(c => new Category
            {
                CategoryName = c.CategoryName,
                ParentCategory = c.ParentCategory,
            });

            var tags = await _tagService.GetAllAsync();
            var tagViewModels = tags.Select(t => new Tag { TagName = t.TagName });

            var authors = newsArticles
                ?.Where(n => n.CreatedBy?.AccountId != null)
                .Select(n => n.CreatedBy)
                .Distinct();

            var viewModel = new NewsIndexViewModel
            {
                NewsArticles = newsArticles,
                Categories = categoryViewModels,
                Tags = tagViewModels,
                Authors = authors,
            };

            return View(viewModel);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    public class NewsIndexViewModel
    {
        public IEnumerable<NewsArticle> NewsArticles { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<Tag> Tags { get; set; }
        public IEnumerable<SystemAccount> Authors { get; set; }
    }
}
