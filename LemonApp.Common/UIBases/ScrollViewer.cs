using EleCho.WpfSuite;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LemonApp.Common.UIBases
{
    public class ScrollViewer : System.Windows.Controls.ScrollViewer
    {
        /// <summary>
        /// 精确滚动模型，指定目标偏移
        /// </summary>
        private double _targetOffset = 0;
        /// <summary>
        /// 缓动滚动模型，指定目标速度
        /// </summary>
        private double _targetVelocity = 0;

        /// <summary>
        /// 缓动模型的叠加速度力度
        /// </summary>
        private const double VelocityFactor = 1.2;
        /// <summary>
        /// 缓动模型的速度衰减系数，数值越小，滚动越慢
        /// </summary>
        private const double Friction = 0.96;

        /// <summary>
        /// 精确模型的插值系数，数值越大，滚动越快接近目标
        /// </summary>
        private const double LerpFactor = 0.3;

        public ScrollViewer()
        {
            _currentOffset = VerticalOffset;

            this.IsManipulationEnabled = true;
            this.PanningMode = PanningMode.VerticalOnly;
           // this.PanningDeceleration = 0; // 禁用默认惯性

            StylusTouchDevice.SetSimulate(this, true);

            DependencyPropertyDescriptor
                    .FromProperty(VerticalOffsetProperty, typeof(System.Windows.Controls.ScrollViewer))
                    .AddValueChanged(this, HandleExternalScrollChanged);

            Unloaded += ScrollViewer_Unloaded;
        }
        //记录参数
        private int _lastScrollingTick = 0, _lastScrollDelta = 0;
        private double _lastTouchVelocity = 0;
        private double _currentOffset = 0;
        //标志位
        private bool _isRenderingHooked = false;
        private bool _isAccuracyControl = false;
        private bool _isInternalScrollChange = false;

        private void ScrollViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            DependencyPropertyDescriptor
                .FromProperty(VerticalOffsetProperty, typeof(System.Windows.Controls.ScrollViewer))
                .RemoveValueChanged(this, HandleExternalScrollChanged);

            if (_isRenderingHooked)
            {
                CompositionTarget.Rendering -= OnRendering;
                _isRenderingHooked = false;
            }
        }

        /// <summary>
        /// 处理外部滚动事件，更新当前偏移量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleExternalScrollChanged(object? sender, EventArgs e)
        {
            if (!_isInternalScrollChange)
                _currentOffset = VerticalOffset;
        }

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            base.OnManipulationDelta(e);    //如果没有这一行则不会触发ManipulationCompleted事件??
            e.Handled = true;
            //手还在屏幕上，使用精确滚动
            _isAccuracyControl = true;
            double deltaY = -e.DeltaManipulation.Translation.Y;
            _targetOffset = Math.Clamp(_targetOffset + deltaY, 0, ScrollableHeight);
            // 记录最后一次速度
            _lastTouchVelocity = -e.Velocities.LinearVelocity.Y;

            if (!_isRenderingHooked)
            {
                CompositionTarget.Rendering += OnRendering;
                _isRenderingHooked = true;
            }
        }

        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            base.OnManipulationCompleted(e);
            e.Handled = true;
            Debug.WriteLine("vel: " + _lastTouchVelocity);
            _targetVelocity = _lastTouchVelocity; // 用系统识别的速度继续滚动
            _isAccuracyControl = false;

            if (!_isRenderingHooked)
            {
                CompositionTarget.Rendering += OnRendering;
                _isRenderingHooked = true;
            }
        }

        /// <summary>
        /// 判断MouseWheel事件由鼠标触发还是由触控板触发
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool IsTouchpadScroll(MouseWheelEventArgs e)
        {
            var tickCount = Environment.TickCount;
            var isTouchpadScrolling =
                    e.Delta % Mouse.MouseWheelDeltaForOneLine != 0 ||
                    (tickCount - _lastScrollingTick < 100 && _lastScrollDelta % Mouse.MouseWheelDeltaForOneLine != 0);
            Debug.WriteLine(e.Delta + "  " + e.Timestamp + "  ==>" + isTouchpadScrolling);
            _lastScrollDelta = e.Delta;
            _lastScrollingTick = e.Timestamp;
            return isTouchpadScrolling;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            e.Handled = true;

            //触摸板使用精确滚动模型
            _isAccuracyControl = IsTouchpadScroll(e);

            if (_isAccuracyControl)
            {
                _targetVelocity = 0; // 防止下一次触发缓动模型时继承没有消除的速度，造成滚动异常
                _targetOffset = Math.Clamp(_currentOffset - e.Delta, 0, ScrollableHeight);
            }
            else
                _targetVelocity += -e.Delta * VelocityFactor;// 鼠标滚动，叠加速度（惯性滚动）

            if (!_isRenderingHooked)
            {
                CompositionTarget.Rendering += OnRendering;
                _isRenderingHooked = true;
            }
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (_isAccuracyControl)
            {
                // 精确滚动：Lerp 逼近目标
                _currentOffset += (_targetOffset - _currentOffset) * LerpFactor;

                // 如果已经接近目标，就停止
                if (Math.Abs(_targetOffset - _currentOffset) < 0.5)
                {
                    _currentOffset = _targetOffset;
                    StopRendering();
                }
            }
            else
            {
                // 缓动滚动：速度衰减模拟
                if (Math.Abs(_targetVelocity) < 0.1)
                {
                    _targetVelocity = 0;
                    StopRendering();
                    return;
                }

                _targetVelocity *= Friction;
                _currentOffset = Math.Clamp(_currentOffset + _targetVelocity * (1.0 / 60), 0, ScrollableHeight);
            }

            InternalScrollToVerticalOffset(_currentOffset);
        }

        private void InternalScrollToVerticalOffset(double offset)
        {
            _isInternalScrollChange = true;
            ScrollToVerticalOffset(offset);
            _isInternalScrollChange = false;
        }


        private void StopRendering()
        {
            CompositionTarget.Rendering -= OnRendering;
            _isRenderingHooked = false;
        }
    }

}