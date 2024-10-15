using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using LemonApp.Common.Configs;
using LemonApp.Common.UIBases;
using LemonApp.Services;
using LemonApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindowBase
    {
        public MainWindow( 
           MainWindowViewModel mainWindowViewModel,
            IServiceProvider serviceProvider,
            UIResourceService uiResourceService,
            AppSettingsService appSettingsService
        )
        {
            InitializeComponent();
            DataContext = _vm = mainWindowViewModel;
            _serviceProvider = serviceProvider;
            _uiResourceService = uiResourceService;
            _appSettingsService = appSettingsService;
        }
        private readonly MainWindowViewModel _vm;
        private readonly IServiceProvider _serviceProvider;
        private readonly UIResourceService _uiResourceService;
        private readonly AppSettingsService _appSettingsService;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            loginWindow.OnLogin = (auth) =>
            {
                MessageBox.Show(auth.Cookie);
            };
            loginWindow.Show();
        }

        private void UserProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            var popup = _serviceProvider.GetRequiredService<UserMenuPopupWindow>();
            //获取控件相对于窗口位置
            var point = UserProfileBtn.TranslatePoint(new Point(0, 0), this);
            popup.Left =this.Left+ point.X-popup.Width+UserProfileBtn.Width;
            popup.Top =this.Top+ point.Y + UserProfileBtn.ActualHeight;
            popup.Show();
        }
        private Storyboard? _openLyricPageAni, _closeLyricPageAni;

        private async void AudioBtn_Click(object sender, RoutedEventArgs e)
        {
            await Task.Yield();
            testPopup.IsOpen = true;
        }

        private void MusicControl_Info_Click(object sender, RoutedEventArgs e)
        {
            _openLyricPageAni ??= (Storyboard)Resources["OpenLyricPageAni"];
            _closeLyricPageAni ??= (Storyboard)Resources["CloseLyricPageAni"];
            if (_vm.IsLyricPageOpen)
            {
                _closeLyricPageAni.Begin();
                _vm.IsLyricPageOpen = false;
            }
            else
            {
                var point = MusicControl_Img.TranslatePoint(new Point(0, 0), this);
                (_closeLyricPageAni.Children[4] as ThicknessAnimationUsingKeyFrames)!.KeyFrames[0].Value =
                (_openLyricPageAni.Children[6] as ThicknessAnimationUsingKeyFrames)!.KeyFrames[0].Value =
                            new Thickness(point.X, (double)656 / 681 * point.Y, 0, 0);
                _openLyricPageAni.Begin();
                _vm.IsLyricPageOpen=true;

            }
        }
        // private void Border_MouseDown(object sender, MouseButtonEventArgs e){
        //     _appSettingsService.GetConfigMgr<Appearence>().Data.AccentColorMode=Appearence.AccentColorType.Custome;
        //     _appSettingsService.GetConfigMgr<Appearence>().Data.AccentColor=Colors.LightYellow;

        //     _uiResourceService.UpdateAccentColor();
        // }
    }
}