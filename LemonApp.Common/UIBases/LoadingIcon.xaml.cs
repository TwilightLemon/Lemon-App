using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LemonApp.Common.UIBases
{
    /// <summary>
    /// 一个简单的加载动画
    /// </summary>
    public partial class LoadingIcon : UserControl
    {
        public LoadingIcon()
        {
            InitializeComponent();
            IsVisibleChanged += LoadingIcon_IsVisibleChanged;
        }

        CancellationTokenSource? _cts = null;
        Storyboard? loadingAni = null;
        private async void LoadingIcon_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            loadingAni ??= Resources["LoadingAni"] as Storyboard;
            if (IsVisible)
            {
                if (_cts != null) return;
                _cts = new CancellationTokenSource();
                loadingAni?.Begin();
                await Animate(_cts.Token);
            }
            else
            {
                loadingAni?.Stop();
                _cts?.Cancel();
                _cts = null;
            }
        }

        private async Task Animate(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    for (double x = 8; x <= 16; x += 0.1)
                    {
                        ellipse.StrokeDashArray[0] = x;
                        await Task.Delay(10, cancellationToken);
                    }
                    await Task.Delay(110, cancellationToken);
                    for (double x = 16; x >= 8; x -= 0.1)
                    {
                        ellipse.StrokeDashArray[0] = x;
                        await Task.Delay(10, cancellationToken);
                    }
                    await Task.Delay(110, cancellationToken);
                }
            }
            catch(OperationCanceledException)
            {
                //ignore
            }
        }
    }
}
