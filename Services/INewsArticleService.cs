using BusinessObjects.Models;

namespace Services;

public interface INewsArticleService : IService<NewsArticle, string>
{
    Task<IEnumerable<NewsArticle>?> GetAllActiveNews();
    Task<IEnumerable<NewsArticle>> GetScheduledArticles();
    Task<IEnumerable<NewsArticle>> GetAllNewsByCreatedId(short id);
}
