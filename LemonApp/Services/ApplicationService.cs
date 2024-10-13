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
        UIResourceService uiResourceService
        ) : IHostedService
    {
        private readonly ILogger _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly AppSettingsService _appSettingsService = appSettingsService;
        private readonly UIResourceService _uiResourceService = uiResourceService;
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Load settings
            _appSettingsService.LoadAsync((success)=>{
                if(!success){
                    _logger.LogError("Failed to load settings.");
                    return;
                }
                //apply settings
                _uiResourceService.UpdateColorMode();
                _uiResourceService.UpdateAccentColor();
                
                //show main window
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                App.Current.MainWindow = mainWindow;
                mainWindow.Show();
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
