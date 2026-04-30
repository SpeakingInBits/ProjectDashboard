using Microsoft.Extensions.Logging;
using ProjectDashboard.Services;
using ProjectDashboard.ViewModels;
using ProjectDashboard.Views;

namespace ProjectDashboard
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton(sp =>
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ProjectDashboard/1.0");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                return client;
            });
            builder.Services.AddSingleton<GitHubService>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<ProjectSettingsViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<ProjectSettingsPage>();
            builder.Services.AddTransient<DeleteFromGitHubPage>();
            builder.Services.AddTransient<AppShell>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
