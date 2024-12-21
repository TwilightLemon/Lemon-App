using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace LemonApp.Views.UserControls
{
    /// <summary>
    /// SearchHintView.xaml 的交互逻辑
    /// </summary>
    public partial class SearchHintView : UserControl, INotifyPropertyChanged
    {
        public SearchHintView(MainNavigationService mainNavigationService)
        {
            InitializeComponent();
            _mainNavigationService = mainNavigationService;
            DataContext = this;
        }
        private readonly MainNavigationService _mainNavigationService;
        private SearchHint? _hints;
        public Action? RequestClose,RequestDefocus;
        public SearchHint? Hints
        {
            get=>_hints;
            set
            {
                _hints=value;
                OnPropertyChanged(nameof(Hints));
                HintList.SelectedItem = null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Goto()
        {
            if(HintList.SelectedItem is SearchHint.Hint hint)
            {
                switch(hint.Type)
                {
                    case SearchHint.HintType.Music:
                        _mainNavigationService.RequstNavigation(PageType.SearchPage, hint.Content+" "+hint.Singer);
                        break;
                    case SearchHint.HintType.Album:
                        _mainNavigationService.RequstNavigation(PageType.AlbumPage, hint.Id);
                        break;
                    case SearchHint.HintType.Singer:
                        _mainNavigationService.RequstNavigation(PageType.ArtistPage, hint.Id);
                        break;
                    default:
                        break;
                }
                RequestClose?.Invoke();
            }
        }

        private void ListBoxItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item)
            {
                item.IsSelected = true;
                Goto();
            }
        }

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Goto();
            else if (e.Key != Key.Up && e.Key != Key.Down)
                RequestDefocus?.Invoke();
        }
    }
}
