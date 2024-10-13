using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;
using EleCho.WpfSuite;
using LemonApp.Common.WinAPI;
using LemonApp.Common.Behaviors;

namespace LemonApp.Common.UIBases
{
    public class PopupWindowBase:Window
    {
        public PopupWindowBase()
        {
            //Set basic style for popup window
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowOption.SetCorner(this, WindowCorner.Round);
            ShowInTaskbar = false;
            Topmost = true;
            Activate();
            Deactivated += PopWindowBase_Deactivated;
            this.Initialized += PopWindowBase_Initialized;
            this.Loaded += PopWindowBase_Loaded;
            this.ContentRendered += PopWindowBase_ContentRendered;
        }

        /// <summary>
        /// 指示PopupWindow是否总是保持打开，失去焦点时不自动关闭
        /// </summary>
        public bool AlwaysShow
        {
            get { return (bool)GetValue(AlwaysShowProperty); }
            set { SetValue(AlwaysShowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AlwaysShow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AlwaysShowProperty =
            DependencyProperty.Register("AlwaysShow", typeof(bool), typeof(PopupWindowBase), new PropertyMetadata(false));



        private void PopWindowBase_Initialized(object? sender, EventArgs e)
        {
            _openAni = new(0, Height, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase()
            };
            Height = 0;

            //remove local resource dic, reflect ThemeConf to main App domain
            if (Resources.MergedDictionaries.FirstOrDefault(d => d.Source.ToString().Contains("Styles/ThemeColor"))
                is ResourceDictionary defResDic)
            {
                Resources.MergedDictionaries.Remove(defResDic);
            }
        }
        DoubleAnimation _openAni;
        private void PopWindowBase_ContentRendered(object? sender, EventArgs e)
        {
            BeginAnimation(HeightProperty, _openAni);
        }

        private void PopWindowBase_Loaded(object sender, RoutedEventArgs e)
        {
            WindowLongAPI.SetToolWindow(this);
            BehaviorCollection behaviors = Interaction.GetBehaviors(this);
            behaviors.Add(new BlurWindowBehavior());
        }

        private void PopWindowBase_Deactivated(object? sender, EventArgs e)
        {
            if (AlwaysShow) return;
            Close();
        }
    }
}
