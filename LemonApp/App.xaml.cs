using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using LemonApp.Services;
using LemonApp.Views.Windows;
using NLog.Extensions.Logging;
using LemonApp.Common.Configs;
using System.Net.Http;
using LemonApp.ViewModels;
using LemonApp.Views.Pages;

namespace LemonApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost? Host { get; private set; } = null;
        public const string PublicClientFlag = "PublicClient";
        private static void BuildHost()
        {
            var builder = new HostBuilder();
            Host = builder.ConfigureServices(services =>
            {
                services.AddHttpClient(PublicClientFlag)
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip,
                    UseCookies=true,
                    UseProxy = true
                });


                //host
                services.AddSingleton<ApplicationService>();
                services.AddHostedService(p => p.GetRequiredService<ApplicationService>());
                services.AddSingleton<AppSettingsService>();
                services.AddHostedService(
                    p=>p.GetRequiredService<AppSettingsService>()
                        .AddConfig<UserProfile>()
                        .AddConfig<Appearence>()
                );

                //services
                services.AddSingleton<UIResourceService>();
                services.AddSingleton<UserProfileService>();
                services.AddSingleton<MainNavigationService>();

                //window
                services.AddSingleton<MainWindow>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<UserMenuPopupWindow>();

                //pages
                services.AddTransient<SettingsPage>();
                services.AddTransient<HomePage>();
                services.AddTransient<RankPage>();
                services.AddTransient<PlaylistPage>();

                //ViewModels
                services.AddSingleton<MainWindowViewModel>();
                services.AddTransient<UserMenuViewModel>();
                services.AddTransient<PlaylistPageViewModel>();

                //Logger
                services.AddLogging(builder =>
                {
                    builder.AddNLog();
                });

            }).Build();
        }
        public App()
        {
            InitializeComponent();
            BuildHost();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Host!.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _ = Host!.StopAsync();
            base.OnExit(e);
        }

    }

}
