using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Navigation;
using LemonApp.Components;

namespace LemonApp.Services
{
    public class ApplicationService(
         IServiceProvider serviceProvider,
        ILogger<ApplicationService> logger,
        AppSettingsService appSettingsService,
        UIResourceService uiResourceService,
        UserProfileService userProfileService,
        MediaPlayerService mediaPlayerService,
        WindowBasicComponent windowBasicComponent
        ) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            //注册Hyperlink的跳转事件
            EventManager.RegisterClassHandler(typeof(Hyperlink), Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler((sender, e) =>
            {
                System.Diagnostics.Process.Start("explorer.exe", e.Uri.AbsoluteUri);
                e.Handled = true;
            }));
            // Load settings
            appSettingsService.LoadAsync(async(success)=>{
                if(!success){
                    logger.LogError("Failed to load settings.");
                    return;
                }
                //load cache manager
                await CacheManager.LoadPath();

                //init userProfileService
                userProfileService.Init();

                //init media player
                await mediaPlayerService.Init();

                //apply settings
                uiResourceService.UpdateColorMode();
                uiResourceService.UpdateAccentColor();
                
                //show main window
                var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
                App.Current.MainWindow = mainWindow;
                mainWindow.Show();

                //apply theme config
                uiResourceService.UpdateThemeConfig();

                //init window basic components
                windowBasicComponent.Init();

                //check & update user profile
                if (appSettingsService.GetConfigMgr<UserProfile>()?.Data?.TencUserAuth is {Id.Length:>5} auth)
                    await userProfileService.UpdateAuthAndNotify(auth);

            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
