using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Downloader;
using LemonApp.Common.Funcs;
using LemonApp.Common.UIBases;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// UpdaterWindow.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class UpdaterWindow : FluentWindowBase
    {
        public UpdaterWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
        public static async void CheckUpdateAsync()
        {
            try
            {
                var updater = new Updater(App.Services.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag));
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version!;
                if (await updater.CheckUpdateAsync(currentVersion))
                {
                    var window = new UpdaterWindow
                    {
                        Config = updater.Config
                    };
                    window.Show();
                }
            }
            catch { }
        }
        [ObservableProperty]
        private UpdaterConfig? _config;
        [ObservableProperty]
        private string _packageSize = "";
        partial void OnConfigChanged(UpdaterConfig? value)
        {
            if(value is null) return;
            PackageSize =Math.Round( value.ReleaseFileSize / 1024.0 / 1024.0,3) + " MB";
        }
        [RelayCommand]
        private async Task Update()
        {
            if(Config is null) return;
            var zipFile = Path.Combine(CacheManager.GetCachePath(CacheManager.CacheType.Other),"release.zip");
            var extractPath = Path.Combine(CacheManager.GetCachePath(CacheManager.CacheType.Other), "release");
            
            var redirectUrl=await HttpHelper.GetRedirectUrl(Config.DownloadUrl);
            var downloader = new DownloadService(new()
            {
                ChunkCount = 4,
                ParallelDownload = true,
                MaxTryAgainOnFailover = 10,
                ClearPackageOnCompletionWithFailure = true
            }, null);
            downloader.DownloadProgressChanged += (_, e) => {
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = e.ProgressPercentage; });
            };
            await downloader.DownloadFileTaskAsync(redirectUrl, zipFile);
            var zipInfo = new FileInfo(zipFile);
            if (zipInfo.Exists && zipInfo.Length == Config.ReleaseFileSize)
            {
                // Extract the zip file
                if(Directory.Exists(extractPath))
                    Directory.Delete(extractPath,true);
                ZipFile.ExtractToDirectory(zipFile, extractPath);
                // run installer
                var installerPath = Path.Combine(extractPath, "win-release.exe");
                if (File.Exists(installerPath))
                {
                    var process = new Process
                    {
                        StartInfo = new()
                        {
                            FileName = installerPath,
                            UseShellExecute = true
                        }
                    };
                    process.Start();
                    Close();
                    return;
                }
            }
            //failed
            ExMessageBox.Show("Failed to download update package, please refer to the release page on GitHub to update manually.");
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
