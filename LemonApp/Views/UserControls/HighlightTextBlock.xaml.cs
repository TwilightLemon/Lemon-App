using LemonApp.Shaders.Impl;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LemonApp.Views.UserControls;

public partial class HighlightTextBlock : UserControl
{
    private ProgresiveHighlightEffect? _effect = null;

    static HighlightTextBlock()
    {
        // 重写继承的文本属性元数据，以便在属性更改时更新文本裁剪
        FontFamilyProperty.OverrideMetadata(typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, OnTextPropertyChanged));
        FontSizeProperty.OverrideMetadata(typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(14.0, OnTextPropertyChanged));
        FontWeightProperty.OverrideMetadata(typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(FontWeights.Normal, OnTextPropertyChanged));
        FontStyleProperty.OverrideMetadata(typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(FontStyles.Normal, OnTextPropertyChanged));
        FontStretchProperty.OverrideMetadata(typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(FontStretches.Normal, OnTextPropertyChanged));
        ForegroundProperty.OverrideMetadata(typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(Brushes.Black, OnForegroundChanged));
    }
    public bool IsSpiltEnabled { get; init; }
    public HighlightTextBlock() : this(false) { }
    public HighlightTextBlock(bool isSpiltEnabled)
    {
        InitializeComponent();
        Loaded += HighlightTextBlock_Loaded;
        Unloaded += HighlightTextBlock_Unloaded;
        IsSpiltEnabled = isSpiltEnabled;
    }

    private void HighlightTextBlock_Unloaded(object sender, RoutedEventArgs e)
    {
        Loaded -= HighlightTextBlock_Loaded;
        Unloaded -= HighlightTextBlock_Unloaded;
        PART_Rectangle.Effect = null;
        _effect = null;
        CleanupOldClip();
    }

    private void HighlightTextBlock_Loaded(object sender, RoutedEventArgs e)
    {
        if (HighlightPos > 0)
        {
            InitEffect();
        }
    }

    private double _layoutConstraintWidth;
    private double _layoutConstraintHeight;
    private bool _clipGeometryDirty = true;
    private bool _isUpdatingClip = false;
    private string? _lastClipText = null;
    private double _lastClipFontSize;
    private FontFamily? _lastClipFontFamily;
    private FontWeight _lastClipFontWeight;
    private FontStyle _lastClipFontStyle;
    private double _lastClipConstraintWidth;
    private double _lastClipConstraintHeight;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (IsSpiltEnabled)
        {
            // 拆分模式：无视容器限制
            _layoutConstraintWidth = double.PositiveInfinity;
            _layoutConstraintHeight = double.PositiveInfinity;

            if (_clipGeometryDirty)
            {
                UpdateTextClip();
                _clipGeometryDirty = false;
            }
            return base.MeasureOverride(availableSize);
        }
        else
        {
            _layoutConstraintWidth = availableSize.Width;
            _layoutConstraintHeight = availableSize.Height;

            // 非拆分模式：不在 Measure 阶段调用 UpdateTextClip，
            // 否则 PART_Rectangle.Width 被设为文本宽度，导致控件期望尺寸过小，
            // 父容器只分配文本宽度而不是可用宽度。
            // Clip 更新延迟到 ArrangeOverride 中进行。
            if (string.IsNullOrEmpty(Text))
            {
                return new Size(0, 0);
            }

            // 计算文本的自然尺寸，仅用于确定期望高度
            var formattedText = new FormattedText(
                Text,
                CultureInfo.CurrentCulture,
                FlowDirection,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            if (!double.IsNaN(LineHeight) && LineHeight > 0)
                formattedText.LineHeight = LineHeight;

            // 设置换行约束以正确计算高度
            if (TextWrapping != TextWrapping.NoWrap &&
                !double.IsInfinity(availableSize.Width) && availableSize.Width > 0)
            {
                formattedText.MaxTextWidth = availableSize.Width;
            }

            var textWidth = formattedText.WidthIncludingTrailingWhitespace;
            var textHeight = formattedText.Height;

            // 宽度：使用文本自然宽度，但不超过可用宽度
            // 拉伸填满容器应由父容器的 HorizontalAlignment=Stretch 控制
            var desiredWidth = !double.IsInfinity(availableSize.Width) && availableSize.Width > 0
                ? Math.Min(textWidth, availableSize.Width)
                : textWidth;
            var desiredHeight = textHeight;

            return new Size(desiredWidth, desiredHeight);
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // 防止 UpdateCompleteTextClip 设置 Width/Height 导致的重入
        if (_isUpdatingClip)
            return base.ArrangeOverride(finalSize);

        // 当HighlightTextBlock不可见时，其MeasureOverride/ArrangeOverride可能收到0的可用空间，导致保存了0的约束大小
        if (!IsSpiltEnabled && !string.IsNullOrEmpty(Text))
        {
            bool sizeChanged = Math.Abs(finalSize.Width - _layoutConstraintWidth) > 0.5 ||
                Math.Abs(finalSize.Height - _layoutConstraintHeight) > 0.5;

            if (sizeChanged)
            {
                _layoutConstraintWidth = finalSize.Width;
                _layoutConstraintHeight = finalSize.Height;
            }

            // 仅在文本/字体/约束实际变化时重建 Geometry
            if (sizeChanged || _clipGeometryDirty || NeedsClipUpdate())
            {
                _isUpdatingClip = true;
                try
                {
                    CleanupOldClip();
                    UpdateCompleteTextClip();
                    SaveClipState();
                }
                finally
                {
                    _isUpdatingClip = false;
                }
                _clipGeometryDirty = false;
            }
        }
        return base.ArrangeOverride(finalSize);
    }

    private bool NeedsClipUpdate()
    {
        return _lastClipText != Text
            || _lastClipFontSize != FontSize
            || _lastClipFontFamily != FontFamily
            || _lastClipFontWeight != FontWeight
            || _lastClipFontStyle != FontStyle
            || Math.Abs(_lastClipConstraintWidth - _layoutConstraintWidth) > 0.5
            || Math.Abs(_lastClipConstraintHeight - _layoutConstraintHeight) > 0.5;
    }

    private void SaveClipState()
    {
        _lastClipText = Text;
        _lastClipFontSize = FontSize;
        _lastClipFontFamily = FontFamily;
        _lastClipFontWeight = FontWeight;
        _lastClipFontStyle = FontStyle;
        _lastClipConstraintWidth = _layoutConstraintWidth;
        _lastClipConstraintHeight = _layoutConstraintHeight;
    }

    private void InitEffect()
    {
        _effect ??= new()
        {
            HighlightColor = HighlightColor,
            HighlightPos = HighlightPos,
            HighlightWidth = HighlightWidth,
            HighlightIntensity = GetHighlightIntensity(this),
            UseAdditive = UseAdditive
        };
        PART_Rectangle.Effect = _effect;
    }

    #region Text

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(HighlightTextBlock),
            new PropertyMetadata(string.Empty, OnTextPropertyChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    #endregion

    #region TextWrapping

    public static readonly DependencyProperty TextWrappingProperty =
        DependencyProperty.Register(
            nameof(TextWrapping),
            typeof(TextWrapping),
            typeof(HighlightTextBlock),
            new PropertyMetadata(TextWrapping.NoWrap, OnTextPropertyChanged));

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    #endregion

    #region TextAlignment

    public static readonly DependencyProperty TextAlignmentProperty =
        DependencyProperty.Register(
            nameof(TextAlignment),
            typeof(TextAlignment),
            typeof(HighlightTextBlock),
            new PropertyMetadata(TextAlignment.Left, OnTextPropertyChanged));

    public TextAlignment TextAlignment
    {
        get => (TextAlignment)GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    #endregion

    #region TextTrimming

    public static readonly DependencyProperty TextTrimmingProperty =
        DependencyProperty.Register(
            nameof(TextTrimming),
            typeof(TextTrimming),
            typeof(HighlightTextBlock),
            new PropertyMetadata(TextTrimming.None, OnTextPropertyChanged));

    public TextTrimming TextTrimming
    {
        get => (TextTrimming)GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    #endregion

    #region LineHeight

    public static readonly DependencyProperty LineHeightProperty =
        DependencyProperty.Register(
            nameof(LineHeight),
            typeof(double),
            typeof(HighlightTextBlock),
            new PropertyMetadata(double.NaN, OnTextPropertyChanged));

    public double LineHeight
    {
        get => (double)GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    #endregion

    #region HighlightPos

    /// <summary>
    /// 高光中心位置（0~1，允许动画越界）
    /// </summary>
    public static readonly DependencyProperty HighlightPosProperty =
        DependencyProperty.Register(
            nameof(HighlightPos),
            typeof(double),
            typeof(HighlightTextBlock),
            new PropertyMetadata(0.0, OnHighlightPosChanged));

    public double HighlightPos
    {
        get => (double)GetValue(HighlightPosProperty);
        set => SetValue(HighlightPosProperty, value);
    }

    private static void OnHighlightPosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock c)
        {
            if ((double)e.NewValue > LyricLineControl.InitialHighlightPos && c._effect == null)
            {
                c.InitEffect();
            }
            if (c._effect != null)
                c._effect.HighlightPos = (double)e.NewValue;
        }
    }

    #endregion

    #region HighlightWidth

    /// <summary>
    /// 高光宽度 0~1
    /// </summary>
    public static readonly DependencyProperty HighlightWidthProperty =
        DependencyProperty.Register(
            nameof(HighlightWidth),
            typeof(double),
            typeof(HighlightTextBlock),
            new PropertyMetadata(0.4, OnHighlightWidthChanged));

    public double HighlightWidth
    {
        get => (double)GetValue(HighlightWidthProperty);
        set => SetValue(HighlightWidthProperty, value);
    }

    private static void OnHighlightWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock c && c._effect != null)
        {
            c._effect.HighlightWidth = (double)e.NewValue;
        }
    }

    #endregion

    #region HighlightColor

    /// <summary>
    /// 高光颜色
    /// </summary>
    public static readonly DependencyProperty HighlightColorProperty =
        DependencyProperty.Register(
            nameof(HighlightColor),
            typeof(Color),
            typeof(HighlightTextBlock),
            new PropertyMetadata(Color.FromArgb(240, 230, 242, 255), OnHighlightColorChanged));

    public Color HighlightColor
    {
        get => (Color)GetValue(HighlightColorProperty);
        set => SetValue(HighlightColorProperty, value);
    }

    private static void OnHighlightColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock c && c._effect != null)
        {
            c._effect.HighlightColor = (Color)e.NewValue;
        }
    }

    #endregion

    #region Attach Properties
    public bool UseAdditive
    {
        get { return (bool)GetValue(UseAdditiveProperty); }
        set { SetValue(UseAdditiveProperty, value); }
    }

    public static readonly DependencyProperty UseAdditiveProperty =
        DependencyProperty.RegisterAttached(nameof(UseAdditive), typeof(bool), typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, OnUseAdditiveChanged));

    public static bool GetUseAdditive(DependencyObject obj) => (bool)obj.GetValue(UseAdditiveProperty);
    public static void SetUseAdditive(DependencyObject obj, bool value) => obj.SetValue(UseAdditiveProperty, value);

    private static void OnUseAdditiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock c && c._effect != null)
        {
            c._effect.UseAdditive = (bool)e.NewValue;
        }
    }

    public static double GetHighlightIntensity(DependencyObject obj)
    {
        return (double)obj.GetValue(HighlightIntensityProperty);
    }

    public static void SetHighlightIntensity(DependencyObject obj, double value)
    {
        obj.SetValue(HighlightIntensityProperty, value);
    }

    public static readonly DependencyProperty HighlightIntensityProperty =
        DependencyProperty.RegisterAttached("HighlightIntensity", typeof(double), typeof(HighlightTextBlock), new PropertyMetadata(1.0, OnHighlightIntensityChanged));

    private static void OnHighlightIntensityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock c && c._effect != null)
        {
            c._effect.HighlightIntensity = (double)e.NewValue;
        }
    }
    #endregion

    private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock control)
        {
            control.PART_Rectangle.Fill = e.NewValue as Brush;
        }
    }

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock control)
        {
            control._clipGeometryDirty = true;
            control.InvalidateMeasure();
        }
    }

    private void CleanupOldClip()
    {
        try
        {
            if (Geometries != null)
            {
                foreach (var g in Geometries)
                {
                    if (g.Transform != null)
                    {
                        g.Transform.BeginAnimation(TranslateTransform.YProperty, null);
                        g.Transform.BeginAnimation(TranslateTransform.XProperty, null);
                        g.Transform = null;
                    }
                }
                Geometries = null;
            }
        }
        catch { }
        PART_Rectangle.Clip = null;
    }

    private void UpdateTextClip()
    {
        CleanupOldClip();

        if (string.IsNullOrEmpty(Text))
        {
            PART_Rectangle.Width = 0;
            PART_Rectangle.Height = 0;
            return;
        }

        if (IsSpiltEnabled)
            UpdateSpiltTextClip();
        else UpdateCompleteTextClip();
    }

    public Geometry[]? Geometries { get; private set; }
    private void UpdateSpiltTextClip()
    {
        var display = new GeometryGroup();
        double offsetX = 0;
        double height = 0;
        foreach (char c in Text)
        {
            var formattedText = new FormattedText(c.ToString(),
                                                  CultureInfo.CurrentCulture,
                                                  FlowDirection,
                                                  new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                                                  FontSize,
                                                  Brushes.Black,
                                                  VisualTreeHelper.GetDpi(this).PixelsPerDip);
            if (!double.IsNaN(LineHeight) && LineHeight > 0)
            {
                formattedText.LineHeight = LineHeight;
            }
            formattedText.TextAlignment = TextAlignment;
            var textWidth = formattedText.WidthIncludingTrailingWhitespace;

            var geometry = formattedText.BuildGeometry(new Point(offsetX, 0)).Clone();
            display.Children.Add(geometry);
            offsetX += textWidth;
            height = Math.Max(height, formattedText.Height);
        }
        PART_Rectangle.HorizontalAlignment = TextAlignment switch
        {
            TextAlignment.Center => HorizontalAlignment.Center,
            TextAlignment.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };
        PART_Rectangle.Clip = display;
        Geometries = display.Children.ToArray();

        PART_Rectangle.Width = offsetX;
        PART_Rectangle.Height = height;
    }

    private void UpdateCompleteTextClip()
    {
        var formattedText = new FormattedText(
            Text,
            CultureInfo.CurrentCulture,
            FlowDirection,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            Brushes.Black,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        formattedText.TextAlignment = TextAlignment;
        formattedText.Trimming = TextTrimming;
        if (!double.IsNaN(LineHeight) && LineHeight > 0)
        {
            formattedText.LineHeight = LineHeight;
        }

        // 计算约束宽度（用于换行）：优先使用布局系统提供的约束
        var constraintWidth = !double.IsInfinity(_layoutConstraintWidth) && _layoutConstraintWidth > 0
            ? _layoutConstraintWidth : 0;
        //计算约束高度（用于裁剪）
        var constraintHeight = !double.IsInfinity(_layoutConstraintHeight) && _layoutConstraintHeight > 0
            ? _layoutConstraintHeight : 0;

        //当需要裁剪时，设置约束高度
        if (TextTrimming != TextTrimming.None)
        {
            if (constraintHeight < formattedText.Height)
                constraintHeight = formattedText.Height;
            formattedText.MaxTextHeight = constraintHeight;
        }

        // 仅在需要换行(或者不换行，但是需要裁剪)且有约束宽度时设置 MaxTextWidth
        if ((TextWrapping != TextWrapping.NoWrap ||
            (TextWrapping == TextWrapping.NoWrap && TextTrimming != TextTrimming.None)) &&
            constraintWidth > 0)
        {
            formattedText.MaxTextWidth = constraintWidth;
        }

        // 计算文本实际尺寸
        var textWidth = formattedText.WidthIncludingTrailingWhitespace;
        var textHeight = formattedText.Height;

        // 计算容器宽度：NoWrap且无裁剪时使用文本宽度，换行或裁剪使用约束宽度
        var containerWidth = TextWrapping == TextWrapping.NoWrap && TextTrimming == TextTrimming.None
            ? textWidth
            : (constraintWidth > 0 ? constraintWidth : textWidth);
        //计算容器高度：当不换行且无裁剪时使用文本高度，否则使用约束高度
        var containerHeight = TextWrapping == TextWrapping.NoWrap && TextTrimming == TextTrimming.None
            ? textHeight
            : (constraintHeight > 0 ? constraintHeight : textHeight);

        var geometry = formattedText.BuildGeometry(new Point(0, 0));
        if (geometry.CanFreeze) geometry.Freeze();
        PART_Rectangle.Clip = geometry;
        PART_Rectangle.Width = containerWidth;
        PART_Rectangle.Height = containerHeight;

        // 根据对齐方式设置 Rectangle 的水平对齐
        PART_Rectangle.HorizontalAlignment = TextAlignment switch
        {
            TextAlignment.Center => HorizontalAlignment.Center,
            TextAlignment.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };
    }
}