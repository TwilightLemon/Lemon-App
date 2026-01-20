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
    private readonly ProgresiveHighlightEffect _effect;

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
        _effect = new()
        {
            HighlightColor = HighlightColor,
            HighlightPos = HighlightPos,
            HighlightWidth = HighlightWidth
        };
        PART_Rectangle.Effect = _effect;
        Loaded += (_, _) => UpdateTextClip();
        SizeChanged += (_, _) => UpdateTextClip();
        IsSpiltEnabled = isSpiltEnabled;
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
        if (d is HighlightTextBlock c)
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
        if (d is HighlightTextBlock c)
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
        if (d is HighlightTextBlock c)
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
        if (d is HighlightTextBlock c)
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
            control.UpdateTextClip();
        }
    }

    private void UpdateTextClip()
    {
        if (string.IsNullOrEmpty(Text))
        {
            PART_Rectangle.Clip = null;
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

        // 计算约束宽度（用于换行）
        var constraintWidth = !double.IsNaN(Width) && Width > 0
            ? Width
            : (!double.IsInfinity(MaxWidth) && MaxWidth > 0 ? MaxWidth : ActualWidth);

        // 仅在需要换行且有约束宽度时设置 MaxTextWidth
        if (TextWrapping != TextWrapping.NoWrap && constraintWidth > 0)
        {
            formattedText.MaxTextWidth = constraintWidth;
        }

        // 计算文本实际尺寸
        var textWidth = formattedText.WidthIncludingTrailingWhitespace;
        var textHeight = formattedText.Height;

        // 计算容器宽度：NoWrap 时使用文本宽度，换行时使用约束宽度
        var containerWidth = TextWrapping == TextWrapping.NoWrap
            ? textWidth
            : (constraintWidth > 0 ? constraintWidth : textWidth);

        var geometry = formattedText.BuildGeometry(new Point(0, 0));
        PART_Rectangle.Clip = geometry;
        PART_Rectangle.Width = containerWidth;
        PART_Rectangle.Height = textHeight;

        // 根据对齐方式设置 Rectangle 的水平对齐
        PART_Rectangle.HorizontalAlignment = TextAlignment switch
        {
            TextAlignment.Center => HorizontalAlignment.Center,
            TextAlignment.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };
    }
}