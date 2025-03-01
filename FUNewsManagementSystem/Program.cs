using BusinessObjects.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Repositories.RepositoryImpl;
using Services;
using Services.ServiceImpl;
using FUNewsManagementSystem.Services;

namespace FUNewsManagementSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<FunewsManagementContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            );

            builder.Services.AddLogging(logging => logging.AddConsole());

            builder
                .Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme =
                        CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.LoginPath = "/SystemAccounts/Login";
                    options.LogoutPath = "/SystemAccounts/Logout";
                    options.AccessDeniedPath = "/Home/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.SlidingExpiration = true; // Gia hạn nếu người dùng hoạt động
                    options.Cookie.HttpOnly = true; // Bảo mật cookie
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Chỉ dùng HTTPS
                })
                .AddGoogle(options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                    options.ClientSecret = builder.Configuration[
                        "Authentication:Google:ClientSecret"
                    ];
                    options.CallbackPath = "/signin-google";
                });
            ;

            builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            builder.Services.AddScoped<ITagRepository, TagRepository>();
            builder.Services.AddScoped<ISystemAccountRepository, SystemAccountRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<INewsArticleRepository, NewsArticleRepository>();

            builder.Services.AddScoped<INewsArticleService, NewsArticleService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<ITagService, TagService>();
            builder.Services.AddScoped<ISystemAccountService, SystemAccountService>();

            // Đăng ký AutoPublishService như một background service
            builder.Services.AddHostedService<AutoPublishService>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}");

            app.Run();
        }
    }
}
