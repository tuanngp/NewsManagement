using BusinessObjects.Models;
using Services;

namespace FUNewsManagementSystem.Services
{
    public class AutoPublishService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<AutoPublishService> _logger;

        public AutoPublishService(IServiceProvider services, ILogger<AutoPublishService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoPublish Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PublishScheduledArticles(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while publishing scheduled articles");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task PublishScheduledArticles(CancellationToken stoppingToken)
        {
            using (var scope = _services.CreateScope())
            {
                var newsService = scope.ServiceProvider.GetRequiredService<INewsArticleService>();

                var articles = await newsService.GetScheduledArticles();
                foreach (var article in articles)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    try
                    {
                        article.Status = ArticleStatus.Published;
                        await newsService.UpdateAsync(article);

                        _logger.LogInformation(
                            "Successfully published scheduled article: {ArticleId} - {Title}",
                            article.NewsArticleId,
                            article.NewsTitle
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error publishing article {ArticleId} - {Title}",
                            article.NewsArticleId,
                            article.NewsTitle
                        );
                    }
                }
            }
        }
    }
}
