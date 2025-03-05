using System.Data;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace FUNewsManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class StatisticsController : Controller
    {
        private readonly INewsArticleService _newsArticleService;
        private readonly ICategoryService _categoryService;
        private readonly ISystemAccountService _accountService;

        public StatisticsController(
            INewsArticleService newsArticleService,
            ICategoryService categoryService,
            ISystemAccountService accountService
        )
        {
            _newsArticleService = newsArticleService;
            _categoryService = categoryService;
            _accountService = accountService;
        }

        public async Task<IActionResult> Index()
        {
            var newsArticles = await _newsArticleService.GetAllAsync();
            var categories = await _categoryService.GetAllAsync();
            var accounts = await _accountService.GetAllAsync();

            ViewBag.TotalArticles = newsArticles.Count();
            ViewBag.TotalCategories = categories.Count();
            ViewBag.TotalAuthors = accounts.Count(a => a.AccountRole == 2);

            // Thống kê bài viết theo trạng thái
            ViewBag.PublishedArticles = newsArticles.Count(n => n.NewsStatus == true);
            ViewBag.PendingArticles = newsArticles.Count(n => n.NewsStatus == false);

            // Top 5 danh mục có nhiều bài viết nhất
            ViewBag.TopCategories = categories
                .OrderByDescending(c => c.NewsArticles.Count)
                .Take(5)
                .Select(c => new { Name = c.CategoryName, Count = c.NewsArticles.Count });

            // Top 5 tác giả có nhiều bài viết nhất
            ViewBag.TopAuthors = accounts
                .OrderByDescending(a => newsArticles.Count(n => n.CreatedById == a.AccountId))
                .Take(5)
                .Select(a => new
                {
                    Name = a.AccountName,
                    Count = newsArticles.Count(n => n.CreatedById == a.AccountId),
                });

            // Thống kê bài viết theo tháng trong năm hiện tại
            var currentYear = DateTime.Now.Year;
            var monthlyStats = newsArticles
                .Where(n => n.CreatedDate.HasValue && n.CreatedDate.Value.Year == currentYear)
                .GroupBy(n => n.CreatedDate.Value.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .OrderBy(x => x.Month);

            ViewBag.MonthlyStats = monthlyStats;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            var newsArticles = await _newsArticleService.GetAllAsync();

            // Tạo một workbook mới
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("News Articles");

            // Thêm header
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Title";
            worksheet.Cell(1, 3).Value = "Category";
            worksheet.Cell(1, 4).Value = "Author";
            worksheet.Cell(1, 5).Value = "Created Date";
            worksheet.Cell(1, 6).Value = "Status";

            // Thêm data
            var row = 2;
            foreach (var article in newsArticles)
            {
                worksheet.Cell(row, 1).Value = article.NewsArticleId;
                worksheet.Cell(row, 2).Value = article.NewsTitle;
                worksheet.Cell(row, 3).Value = article.Category?.CategoryName;
                worksheet.Cell(row, 4).Value = article.CreatedBy?.AccountName;
                worksheet.Cell(row, 5).Value = article.CreatedDate;
                worksheet.Cell(row, 6).Value = article.NewsStatus == true ? "Published" : "Pending";
                row++;
            }

            // Format as table
            var range = worksheet.Range(1, 1, row - 1, 6);
            range.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            worksheet.Columns().AdjustToContents();

            // Convert to byte array
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"news_articles_{DateTime.Now:yyyyMMdd}.xlsx"
            );
        }
    }
}
