using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.Common.WinAPI;
using LemonApp.Components;
using LemonApp.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.Design;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace LemonApp.Services
{
    //only basic services are injected. make sure AppSettingsService ready.
    public class ApplicationService(
         IServiceProvider serviceProvider,
        ILogger<ApplicationService> logger,
        AppSettingService appSettingsService,
        UIResourceService uiResourceService) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            EncodingProvider provider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(provider);

            //异常捕获+写日志
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            App.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            //register handler for Hyperlink in Settings->About.
            EventManager.RegisterClassHandler(typeof(Hyperlink), Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler((sender, e) =>
            {
                System.Diagnostics.Process.Start("explorer.exe", e.Uri.AbsoluteUri);
                e.Handled = true;
            }));

            var startup = async() =>{
                await CacheManager.LoadPath();
                await serviceProvider.GetRequiredService<MediaPlayerService>().Init();

                //show main window
                var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
                App.Current.MainWindow = mainWindow;
                mainWindow.Show();

                //apply theme config
                uiResourceService.UpdateColorMode();
                uiResourceService.UpdateAccentColor();
                SystemThemeAPI.RegesterOnThemeChanged(mainWindow, OnThemeChanged, OnSystemColorChanged);
                uiResourceService.UpdateThemeConfig();

                //init window basic components
                serviceProvider.GetRequiredService<WindowBasicComponent>().Init();

                //check & update user profile
                if (appSettingsService.GetConfigMgr<UserProfile>().Data.TencUserAuth is { Id.Length: > 5 } auth)
                    _ = serviceProvider.GetRequiredService<UserProfileService>().UpdateAuthAndNotify(auth);
            };
            startup();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Respond to system theme color (dark mode) changed.
        /// </summary>
        private void OnThemeChanged()
        {
            uiResourceService.UpdateColorMode();
            uiResourceService.UpdateAccentColor();
        }
        /// <summary>
        /// Respond to system accent color changed.
        /// </summary>
        private void OnSystemColorChanged()
        {
            uiResourceService.UpdateAccentColor();
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            logger.LogError(new EventId(-1), e.Exception, e.Exception.Message);
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            logger.LogError(new EventId(-1), e.Exception, e.Exception.Message);
#if !DEBUG
            e.Handled = true;
#endif
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is not Exception exception)
                return;

            logger.LogError(new EventId(-1), exception, exception.Message);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
