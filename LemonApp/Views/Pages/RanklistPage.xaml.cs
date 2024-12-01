using LemonApp.ViewModels;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// RanklistPage.xaml 的交互逻辑
    /// </summary>
    public partial class RanklistPage : Page
    {
        public RanklistPage(RanklistPageViewModel ranklistPageViewModel)
        {
            InitializeComponent();
            DataContext = vm = ranklistPageViewModel;
            Loaded += RanklistPage_Loaded;
        }

        private void RanklistPage_Loaded(object sender, RoutedEventArgs e)
        {
            _=vm.LoadData();
        }

        private readonly RanklistPageViewModel vm;

        private void RankList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RankList.SelectedItem = null;
        }
    }
}
