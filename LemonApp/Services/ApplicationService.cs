using LemonApp.Views.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.Services
{
    internal class ApplicationService(
        ILogger<ApplicationService> logger,
        AppSettingsService appSettingsService,
        UIResourceService uiResourceService
        ) : IHostedService
    {
        private readonly ILogger _logger = logger;
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
                
                //show main window
                var mainWindow = new MainWindow();
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
