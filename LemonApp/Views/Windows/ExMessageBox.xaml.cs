using LemonApp.Common.UIBases;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// ExMessageBox.xaml 的交互逻辑
    /// </summary>
    public partial class ExMessageBox : FluentWindowBase
    {
        private ExMessageBox()
        {
            InitializeComponent();
        }
        private ExMessageBox(string text)
        {
            InitializeComponent();
            ContentTb.Text = text;
        }
        public static bool Show(string text)
        {
            return new ExMessageBox(text).ShowDialog() == true;
        }

        private void ConfirmBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
