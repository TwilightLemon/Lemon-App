using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace LemonApp.Shaders.Impl;

public sealed class ProgresiveHighlightEffect : ShaderEffect
{
    #region Shader

    private static readonly PixelShader _pixelShader = new PixelShader
    {
        UriSource = new Uri(
            "pack://application:,,,/LemonApp;component/Shaders/TextGlow.ps",
            UriKind.Absolute)
    };

    #endregion

    #region Constructor

    public ProgresiveHighlightEffect()
    {
        PixelShader = _pixelShader;

        UpdateShaderValue(InputProperty);

        UpdateShaderValue(HighlightPosProperty);
        UpdateShaderValue(HighlightWidthProperty);
        UpdateShaderValue(HighlightColorProperty);
        UpdateShaderValue(UseAdditiveProperty);
        UpdateShaderValue(HighlightIntensityProperty);
    }

    #endregion

    #region Input (s0)

    /// <summary>
    /// 输入纹理
    /// </summary>
    public static readonly DependencyProperty InputProperty =
        RegisterPixelShaderSamplerProperty(
            "Input",
            typeof(ProgresiveHighlightEffect),
            0,
            SamplingMode.NearestNeighbor);

    public Brush Input
    {
        get => (Brush)GetValue(InputProperty);
        set => SetValue(InputProperty, value);
    }

    #endregion

    #region HighlightPos (c0)

    /// <summary>
    /// 高光中心位置（0~1，允许动画越界）
    /// </summary>
    public static readonly DependencyProperty HighlightPosProperty =
        DependencyProperty.Register(
            nameof(HighlightPos),
            typeof(double),
            typeof(ProgresiveHighlightEffect),
            new UIPropertyMetadata(
                0.0,
                PixelShaderConstantCallback(0)));

    public double HighlightPos
    {
        get => (double)GetValue(HighlightPosProperty);
        set => SetValue(HighlightPosProperty, value);
    }

    #endregion

    #region HighlightWidth (c1)

    /// <summary>
    /// 高光宽度 0~1
    /// </summary>
    public static readonly DependencyProperty HighlightWidthProperty =
        DependencyProperty.Register(
            nameof(HighlightWidth),
            typeof(double),
            typeof(ProgresiveHighlightEffect),
            new UIPropertyMetadata(
                0.12,
                PixelShaderConstantCallback(1)));

    public double HighlightWidth
    {
        get => (double)GetValue(HighlightWidthProperty);
        set => SetValue(HighlightWidthProperty, value);
    }

    #endregion

    #region HighlightColor (c2)

    /// <summary>
    /// 高光颜色
    /// </summary>
    public static readonly DependencyProperty HighlightColorProperty =
        DependencyProperty.Register(
            nameof(HighlightColor),
            typeof(Color),
            typeof(ProgresiveHighlightEffect),
            new UIPropertyMetadata(
                Color.FromArgb(255, 230, 242, 255),
                PixelShaderConstantCallback(2)));

    public Color HighlightColor
    {
        get => (Color)GetValue(HighlightColorProperty);
        set => SetValue(HighlightColorProperty, value);
    }

    #endregion

    #region UseAdditive (c3)

    /// <summary>
    /// 是否使用加法混合模式（true = additive，false = lerp）
    /// </summary>
    public static readonly DependencyProperty UseAdditiveProperty =
        DependencyProperty.Register(
            nameof(UseAdditive),
            typeof(double),
            typeof(ProgresiveHighlightEffect),
            new UIPropertyMetadata(
                1.0,
                PixelShaderConstantCallback(3)));

    public bool UseAdditive
    {
        get => (double)GetValue(UseAdditiveProperty) > 0.5;
        set => SetValue(UseAdditiveProperty, value ? 1.0 : 0.0);
    }

    #endregion

    #region HighlightIntensity (c4)



    public double HighlightIntensity
    {
        get { return (double)GetValue(HighlightIntensityProperty); }
        set { SetValue(HighlightIntensityProperty, value); }
    }

    public static readonly DependencyProperty HighlightIntensityProperty =
        DependencyProperty.Register(
            nameof(HighlightIntensity),
            typeof(double),
            typeof(ProgresiveHighlightEffect),
            new UIPropertyMetadata(
                1.0,
                PixelShaderConstantCallback(4)));
    #endregion
}