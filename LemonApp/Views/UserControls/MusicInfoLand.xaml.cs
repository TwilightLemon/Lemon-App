using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.Views.UserControls
{
    /// <summary>
    /// MusicInfoLand.xaml 的交互逻辑
    /// </summary>
    public partial class MusicInfoLand : UserControl
    {
        public MusicInfoLand()
        {
            InitializeComponent();
        }


        public ICommand QualityCommand
        {
            get { return (ICommand)GetValue(QualityCommandProperty); }
            set { SetValue(QualityCommandProperty, value); }
        }

        public static readonly DependencyProperty QualityCommandProperty =
            DependencyProperty.Register("QualityCommand",
                typeof(ICommand), typeof(MusicInfoLand),
                new PropertyMetadata(null, OnQualityCommandChanged));

        public static void OnQualityCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is MusicInfoLand land&&e.NewValue is ICommand command)
            {
                land.QualityIcon.Command = command;
                land.QualityIcon.IsEnabled = true;
                land.QualityIcon.Visibility = Visibility.Visible;
                if (land.Quality == MusicQuality.Std)
                {
                    land.QualityIcon.SetResourceReference(BorderBrushProperty, "ForeColor");
                    land.QualityText.SetResourceReference(ForegroundProperty, "ForeColor");
                }
            }
        }

        public MusicQuality Quality
        {
            get { return (MusicQuality)GetValue(QualityProperty); }
            set { SetValue(QualityProperty, value); }
        }

        public static readonly DependencyProperty QualityProperty =
            DependencyProperty.Register("Quality", 
                typeof(MusicQuality), typeof(MusicInfoLand), 
                new PropertyMetadata(MusicQuality.SQ, OnQualityChanged));

        public static void OnQualityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is MusicInfoLand land&&e.NewValue is MusicQuality q)
            {
                land.QualityIcon.Visibility = Visibility.Visible;
                land.QualityText.Text = q.ToString();
                if (q == MusicQuality.SQ)
                {
                    land.QualityIcon.DisabledBorderBrush=land.QualityText.Foreground = land.QualityIcon.BorderBrush = SQColor;
                }
                else if (q == MusicQuality.HQ)
                {
                    land.QualityIcon.DisabledBorderBrush = land.QualityText.Foreground = land.QualityIcon.BorderBrush = HQColor;
                }
                else
                {
                    if (land.QualityIcon.IsEnabled)
                    {
                        land.QualityIcon.SetResourceReference(BorderBrushProperty, "ForeColor");
                        land.QualityText.SetResourceReference(ForegroundProperty, "ForeColor");
                    }
                    else land.QualityIcon.Visibility = Visibility.Collapsed;
                }
            }
        }

        readonly static SolidColorBrush SQColor = new(Color.FromRgb(0xF6, 0xB3, 0x22));
        readonly static SolidColorBrush HQColor = new(Color.FromRgb(0x29, 0xDF, 0x96));



        public string MvId
        {
            get { return (string)GetValue(MvIdProperty); }
            set { SetValue(MvIdProperty, value); }
        }

        public static readonly DependencyProperty MvIdProperty =
            DependencyProperty.Register("MvId",
                typeof(string), typeof(MusicInfoLand),
                new PropertyMetadata(null, OnMvIdChanged));

        public static void OnMvIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is MusicInfoLand land)
            {
                land.MVBtn.Visibility = e.NewValue is string { Length: > 0 } ? Visibility.Visible : Visibility.Collapsed;
            }
        }

    }
}
