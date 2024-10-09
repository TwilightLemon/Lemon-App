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
        ILogger<ApplicationService> logger
        ) : IHostedService
    {
        private readonly ILogger _logger = logger;
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var mainWindow = new MainWindow();
            App.Current.MainWindow = mainWindow;
            mainWindow.Show();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
