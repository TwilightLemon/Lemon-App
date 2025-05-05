using LemonApp.Common.Configs;
using LemonApp.Components;
using LemonApp.MusicLib.Media;
using LemonApp.Services;
using LemonApp.ViewModels;
using LemonApp.Views.Pages;
using LemonApp.Views.UserControls;
using LemonApp.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace LemonApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost Host { get; init; }
        public const string PublicClientFlag = "PublicClient";
        public static new App Current => (App)Application.Current;
        public static IServiceProvider Services => Current.Host.Services;
        public new MainWindow MainWindow { get; set; }

        // 定义重试策略
        internal static AsyncRetryPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        private void BuildHost(IServiceCollection services)
        {
            services.AddHttpClient(PublicClientFlag)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip,
                UseCookies = true,
                UseProxy = true
            }).AddPolicyHandler(retryPolicy);


            //host
            services.AddSingleton<AppSettingService>();
            services.AddHostedService(
                p => p.GetRequiredService<AppSettingService>()
                    .AddConfig<UserProfile>()
                    .AddConfig<Appearance>()
                    .AddConfig<PlayingPreference>()
                    .AddConfig<PlaylistCache>(Common.Funcs.Settings.sType.Cache)
                    .AddConfig<LocalPlaylist>(Common.Funcs.Settings.sType.Cache)
                    .AddConfig<DownloadPreference>()
                    .AddConfig<DesktopLyricOption>()
                    .AddConfig<LyricOption>()
            );

            services.AddSingleton<ApplicationService>();
            services.AddHostedService(p => p.GetRequiredService<ApplicationService>());

            services.AddSingleton<DownloadService>();
            services.AddHostedService(p => p.GetRequiredService<DownloadService>());

            services.AddSingleton<MyToolBarLyricClient>();
            services.AddHostedService(p => p.GetRequiredService<MyToolBarLyricClient>());


            //services
            services.AddSingleton<UIResourceService>();
            services.AddSingleton<UserProfileService>();
            services.AddSingleton<MainNavigationService>();
            services.AddSingleton<MediaPlayerService>();
            services.AddSingleton<UserDataManager>();
            services.AddSingleton<ImageCacheService>();
            services.AddSingleton<LocalDissService>();

            //window
            services.AddSingleton<MainWindow>();
            services.AddTransient<LoginWindow>();
            services.AddTransient<UserMenuPopupWindow>();
            services.AddTransient<NotifyIconMenuWindow>();
            services.AddTransient<DesktopLyricWindow>();

            //pages
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<HomePage>();
            services.AddSingleton<RanklistPage>();
            services.AddTransient<PlaylistPage>();
            services.AddSingleton<MyDissPage>();
            services.AddSingleton<MyBoughtPage>();
            services.AddSingleton<AccountInfoPage>();
            services.AddSingleton<DownloadPage>();

            //UI Components
            services.AddSingleton<LyricView>();
            services.AddSingleton<SearchHintView>();
            services.AddSingleton<DownloadMenuDecorator>();

            //MusicLib Components
            services.AddSingleton<SharedLaClient>();

            //MainWindow Components
            services.AddSingleton<WindowBasicComponent>();
            services.AddSingleton<PlaylistDataWrapper>();
            services.AddSingleton<PublicPopupMenuHolder>();
            services.AddSingleton<PopupSelector>();

            //ViewModels
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<UserMenuViewModel>();
            services.AddTransient<PlaylistPageViewModel>();
            services.AddTransient<PlaylistItemViewModel>();
            services.AddTransient<NotifyIconMenuViewModel>();
            services.AddTransient<AlbumItemViewModel>();
            services.AddSingleton<DesktopLyricWindowViewModel>();
            services.AddSingleton<AccountInfoPageViewModel>();
            services.AddSingleton<DownloadPageViewModel>();
            services.AddTransient<SettingsPageViewModel>();
            services.AddTransient<RanklistPageViewModel>();

            //Logger
            services.AddLogging(builder =>
            {
                builder.AddNLog();
            });
        }
        public App()
        {
            InitializeComponent();
            var builder = new HostBuilder();
            Host = builder.ConfigureServices(BuildHost).Build();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Host.Start();
        }

        public new void Shutdown()
        {
            try
            {
                Host.StopAsync().Wait();
                Debug.WriteLine("Hosts stoped.");
                base.Shutdown();
            }
            catch
            {
                Environment.Exit(-1);
            }
        }

    }

}
