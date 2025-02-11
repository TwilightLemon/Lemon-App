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
using LemonApp.Views.UserControls;
using System.Diagnostics;
using System;
using LemonApp.MusicLib.Media;
using LemonApp.Components;
using Polly.Extensions.Http;
using Polly.Retry;
using Polly;

namespace LemonApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost? Host { get; private set; } = null;
        public const string PublicClientFlag = "PublicClient";
        public static new App Current => (App)Application.Current;
        public static IServiceProvider Services => Host!.Services;
        public new MainWindow MainWindow { get; set; }

        // 定义重试策略
        internal static AsyncRetryPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        private static void BuildHost()
        {
            var builder = new HostBuilder();
            Host = builder.ConfigureServices(services =>
            {
                services.AddHttpClient(PublicClientFlag)
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip,
                    UseCookies=true,
                    UseProxy = true
                }).AddPolicyHandler(retryPolicy);


                //host
                services.AddSingleton<ApplicationService>();
                services.AddHostedService(p => p.GetRequiredService<ApplicationService>());

                services.AddSingleton<AppSettingsService>();
                services.AddHostedService(
                    p=>p.GetRequiredService<AppSettingsService>()
                        .AddConfig<UserProfile>()
                        .AddConfig<Appearance>()
                        .AddConfig<PlayingPreference>()
                        .AddConfig<PlaylistCache>(Common.Funcs.Settings.sType.Cache)
                        .AddConfig<DesktopLyricOption>()
                        .AddConfig<LyricOption>()
                );

                //services
                services.AddSingleton<UIResourceService>();
                services.AddSingleton<UserProfileService>();
                services.AddSingleton<MainNavigationService>();
                services.AddSingleton<MediaPlayerService>();
                services.AddSingleton<UserDataManager>();

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

                //UI Components
                services.AddSingleton<LyricView>();
                services.AddSingleton<SearchHintView>();

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
                services.AddTransient<SettingsPageViewModel>();
                services.AddTransient<RanklistPageViewModel>();

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

        public new void Shutdown()
        {
            try
            {
                Host!.StopAsync().Wait();
                Debug.WriteLine("Hosts stoped.");
                base.Shutdown();
            }
            catch
            {
                Environment.Exit(0);
            }
        }

    }

}
