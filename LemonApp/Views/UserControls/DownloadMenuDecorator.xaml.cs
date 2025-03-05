using LemonApp.Services;
using System.Windows.Controls;
using System.Windows;

namespace LemonApp.Views.UserControls
{
    /// <summary>
    /// DownloadMenuDecorator.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadMenuDecorator : UserControl
    {
        public DownloadMenuDecorator(DownloadService downloadService)
        {
            InitializeComponent();
            _downloadService = downloadService;
            Update();
            downloadService.OnDownloadTaskStateChanged += DownloadService_OnDownloadTaskStateChanged;
        }
        void Update(bool isRunning=false)
        {
            Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
            TaskCountTb.Text = _downloadService.TaskCount.ToString();
        }
        private void DownloadService_OnDownloadTaskStateChanged(bool isRunning)
        {
            Dispatcher.Invoke(() => { Update(isRunning); });
            
        }

        private readonly DownloadService _downloadService;
    }
}
