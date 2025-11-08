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
        private const double LerpFactor = 0.5;

        /// <summary>
        /// 上一帧的时间戳
        /// </summary>
        private long _lastTimestamp = 0;

        /// <summary>
        /// 目标帧率（用于归一化计算）
        /// </summary>
        private const double TargetFps = 144;

        /// <summary>
        /// 最小偏移量变化阈值，避免不必要的滚动更新
        /// </summary>
        private const double MinOffsetChange = 0.01;

        /// <summary>
        /// 精确滚动停止阈值
        /// </summary>
        private const double AccuracyStopThreshold = 0.5;

        /// <summary>
        /// 速度停止阈值
        /// </summary>
        private const double VelocityStopThreshold = 0.1;

        /// <summary>
        /// 最大时间跳跃限制（秒），防止窗口失焦后的大跳跃
        /// </summary>
        private const double MaxDeltaTime = 0.1;

        /// <summary>
        /// 预计算的摩擦力常数
        /// </summary>
        private static readonly double OneMinusLerpFactor = 1.0 - LerpFactor;

        public ScrollViewer()
        {
            _currentOffset = VerticalOffset;

            this.IsManipulationEnabled = true;
            this.PanningMode = PanningMode.VerticalOnly;
            // this.PanningDeceleration = 0; // 禁用默认惯性

            StylusTouchDevice.SetSimulate(this, true);

            // 使用 ScrollChanged 事件替代 DependencyPropertyDescriptor（性能更好）
            this.ScrollChanged += OnScrollChanged;

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
            this.ScrollChanged -= OnScrollChanged;

            if (_isRenderingHooked)
            {
                CompositionTarget.Rendering -= OnRendering;
                _isRenderingHooked = false;
            }
        }

        /// <summary>
        /// 处理外部滚动事件，更新当前偏移量
        /// </summary>
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!_isInternalScrollChange && e.VerticalChange != 0)
            {
                _currentOffset = VerticalOffset;
            }
        }

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            base.OnManipulationDelta(e);    //如果没有这一行则不会触发ManipulationCompleted事件??
            e.Handled = true;
            //手还在屏幕上，使用精确滚动
            _isAccuracyControl = true;
            double deltaY = -e.DeltaManipulation.Translation.Y;

            // 跳过微小的变化，减少不必要的更新
            if (Math.Abs(deltaY) < MinOffsetChange)
                return;

            _targetOffset = Math.Clamp(_currentOffset + deltaY, 0, ScrollableHeight);
            // 记录最后一次速度
            _lastTouchVelocity = -e.Velocities.LinearVelocity.Y;

            if (!_isRenderingHooked)
            {
                _lastTimestamp = Stopwatch.GetTimestamp();
                CompositionTarget.Rendering += OnRendering;
                _isRenderingHooked = true;
            }
        }

        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            base.OnManipulationCompleted(e);
            e.Handled = true;
            _targetVelocity = _lastTouchVelocity; // 用系统识别的速度继续滚动
            _isAccuracyControl = false;

            if (!_isRenderingHooked)
            {
                _lastTimestamp = Stopwatch.GetTimestamp();
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
                _lastTimestamp = Stopwatch.GetTimestamp();
                CompositionTarget.Rendering += OnRendering;
                _isRenderingHooked = true;
            }
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            // 计算时间增量（deltaTime）
            long currentTimestamp = Stopwatch.GetTimestamp();
            double deltaTime = (double)(currentTimestamp - _lastTimestamp) / Stopwatch.Frequency;
            _lastTimestamp = currentTimestamp;

            // 限制最大时间跳跃，防止窗口失焦后的异常行为
            if (deltaTime > MaxDeltaTime)
                deltaTime = MaxDeltaTime;

            // 归一化到目标帧率
            double targetFrameTime = 1.0 / TargetFps;
            double timeFactor = deltaTime / targetFrameTime;

            double newOffset = _currentOffset;

            if (_isAccuracyControl)
            {
                // 精确滚动：Lerp 逼近目标（使用时间因子调整）
                // 优化: 使用预计算的常数和更高效的指数计算
                double difference = _targetOffset - _currentOffset;

                // 如果已经接近目标，直接设置并停止
                if (Math.Abs(difference) < AccuracyStopThreshold)
                {
                    newOffset = _targetOffset;
                    StopRendering();
                }
                else
                {
                    // 使用优化的指数衰减计算
                    double lerpAmount = 1.0 - Math.Pow(OneMinusLerpFactor, timeFactor);
                    newOffset = _currentOffset + difference * lerpAmount;
                }
            }
            else
            {
                // 缓动滚动：速度衰减模拟（使用时间因子调整）
                if (Math.Abs(_targetVelocity) < VelocityStopThreshold)
                {
                    _targetVelocity = 0;
                    StopRendering();
                    return;
                }

                // 使用时间因子调整摩擦力衰减
                _targetVelocity *= Math.Pow(Friction, timeFactor);

                // 根据实际时间计算偏移量
                newOffset = _currentOffset + _targetVelocity * (timeFactor / 24);
                newOffset = Math.Clamp(newOffset, 0, ScrollableHeight);
            }

            // 只有当偏移量变化足够大时才更新，避免不必要的布局刷新
            if (Math.Abs(newOffset - _currentOffset) >= MinOffsetChange)
            {
                _currentOffset = newOffset;
                InternalScrollToVerticalOffset(_currentOffset);
            }
        }

        private void InternalScrollToVerticalOffset(double offset)
        {
            _isInternalScrollChange = true;
            ScrollToVerticalOffset(offset);
            _isInternalScrollChange = false;
        }


        private void StopRendering()
        {
            if (_isRenderingHooked)
            {
                CompositionTarget.Rendering -= OnRendering;
                _isRenderingHooked = false;
            }
        }
    }

}