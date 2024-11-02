using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LemonApp.Services
{
    public class ApplicationService(
         IServiceProvider serviceProvider,
        ILogger<ApplicationService> logger,
        AppSettingsService appSettingsService,
        UIResourceService uiResourceService,
        UserProfileService userProfileService,
        MediaPlayerService mediaPlayerService
        ) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Load settings
            appSettingsService.LoadAsync(async(success)=>{
                if(!success){
                    logger.LogError("Failed to load settings.");
                    return;
                }
                //load cache manager
                await CacheManager.LoadPath();

                //init media player
                await mediaPlayerService.Init();

                //apply settings
                uiResourceService.UpdateColorMode();
                uiResourceService.UpdateAccentColor();
                
                //show main window
                var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
                App.Current.MainWindow = mainWindow;
                mainWindow.Show();

                //check & update user profile
                if (appSettingsService.GetConfigMgr<UserProfile>()?.Data?.TencUserAuth is { } auth)
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
