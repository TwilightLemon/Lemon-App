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
        public new MainWindow MainWindow { get; set; }
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
                });


                //host
                services.AddSingleton<ApplicationService>();
                services.AddHostedService(p => p.GetRequiredService<ApplicationService>());

                services.AddSingleton<AppSettingsService>();
                services.AddHostedService(
                    p=>p.GetRequiredService<AppSettingsService>()
                        .AddConfig<UserProfile>()
                        .AddConfig<Appearence>()
                        .AddConfig<PlayingPreference>()
                        .AddConfig<PlaylistCache>(Common.Funcs.Settings.sType.Cache)
                        .AddConfig<DesktopLyricOption>()
                );

                //services
                services.AddSingleton<UIResourceService>();
                services.AddSingleton<UserProfileService>();
                services.AddSingleton<MainNavigationService>();
                services.AddSingleton<MediaPlayerService>();

                //window
                services.AddSingleton<MainWindow>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<UserMenuPopupWindow>();
                services.AddTransient<NotifyIconMenuWindow>();
                services.AddTransient<DesktopLyricWindow>();

                //pages
                services.AddSingleton<SettingsPage>();
                services.AddTransient<HomePage>();
                services.AddTransient<RanklistPage>();
                services.AddTransient<PlaylistPage>();
                services.AddTransient<MyDissPage>();
                services.AddTransient<MyBoughtPage>();
                services.AddSingleton<AccountInfoPage>();

                //UI Components
                services.AddSingleton<LyricView>();
                services.AddSingleton<SearchHintView>();

                //MusicLib Components
                services.AddSingleton<SharedLaClient>();

                //MainWindow Components
                services.AddSingleton<WindowBasicComponent>();
                services.AddSingleton<PlaylistDataWrapper>();

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
