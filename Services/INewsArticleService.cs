using BusinessObjects.Models;

namespace Services.ServiceImpl;

public interface INewsArticleService : IService<NewsArticle, string>
{
    Task<IEnumerable<NewsArticle>?> GetAllActiveNews();
}
