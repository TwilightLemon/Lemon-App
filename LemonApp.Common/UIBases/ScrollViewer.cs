using EleCho.WpfSuite;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace LemonApp.Common.UIBases
{
    public class ScrollViewer : System.Windows.Controls.ScrollViewer
    {
        public ScrollViewer()
        {
            this.FocusVisualStyle = null;
            this.PreviewMouseUp += MyScrollView_PreviewMouseUp;
            this.PanningMode= PanningMode.VerticalOnly;
            this.SnapsToDevicePixels = true;
        }

        private void MyScrollView_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //响应鼠标操作:  手动滑动滚动条的时候更新位置
            LastLocation = VerticalOffset;
        }

        public double LastLocation = 0;
        public new void ScrollToVerticalOffset(double offset)
        {
            AnimateScroll(offset);
            LastLocation = offset;
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            double newOffset = LastLocation - e.Delta;
            base.ScrollToVerticalOffset(LastLocation);
            if (newOffset < 0)
                newOffset = 0;
            if (newOffset > ScrollableHeight)
                newOffset = ScrollableHeight;
            AnimateScroll(newOffset);
            LastLocation = newOffset;
            e.Handled = true;
        }

        private void AnimateScroll(double ToValue)
        {
            BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, null);
            DoubleAnimation Animation = new DoubleAnimation();
            Animation.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut };
            Animation.From = VerticalOffset;
            Animation.To = ToValue;
            Animation.Duration = TimeSpan.FromMilliseconds(300);
            //Timeline.SetDesiredFrameRate(Animation, 40);
            BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, Animation);
        }
    }
}