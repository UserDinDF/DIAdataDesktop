using DIAdataDesktop.Models;
using DIAdataDesktop.ViewModels;
using DIAdataDesktop.Views;
using DIAdataDesktop.Views.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;

namespace DIAdataDesktop
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly Stack<object?> _contentHistory = new();

        private readonly Dictionary<string, UIElement> _pageCache = new();
        private WatchlistWidgetWindow? _widgetWin;

        public MainWindow()
        {
            InitializeComponent();

            _vm = App.Services.GetRequiredService<MainViewModel>();
            DataContext = _vm;

            NavigateTo("Homepage");
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb && tb.Tag is string key)
            {
                _vm.SelectedNav = key;  
                NavigateTo(key);
            }
        }

        private async void NavigateTo(string key)
        {
            UIElement page = key switch
            {
                "Homepage" => await BuildHomePage(),
                "QuotedAssets" => await BuildQuotedAssetsPage(),
                "Exchanges" => BuildExchangesPage(),
                "RealWorldAssets" => await BuildRWAPage(),

                "DataLibrary" => BuildPlaceholder("Data library", "Choose a module on the left."),

                "DigitalAssets" => await BuildQuotedAssetsPage(),
                _ => BuildPlaceholder("Unknown", key)
            };

            _pageCache[key] = page;
            MainContent.Content = page;
        }

        private Task<UIElement> BuildRWAPage()
        {

            var ctrl = App.Services.GetRequiredService<RwaControl>();
            ctrl.DataContext = _vm.RwaVm;
            return Task.FromResult<UIElement>(ctrl);

        }

        private Task<UIElement> BuildHomePage()
        {
            var ctrl = App.Services.GetRequiredService<StartPageControl>();
            ctrl.DataContext = _vm.StartPageVm;
            return Task.FromResult<UIElement>(ctrl);
        }

        private UIElement BuildExchangesPage()
        {
            var ctrl = App.Services.GetRequiredService<ExchangesControl>();
            ctrl.DataContext = _vm.ExchangesVm;
            return ctrl;
        }

        private Task<UIElement> BuildQuotedAssetsPage()
        {
            var ctrl = App.Services.GetRequiredService<QuotedAssetsControl>();
            ctrl.DataContext = _vm.QuotedAssets;
            return Task.FromResult<UIElement>(ctrl);
        }


        private UIElement BuildPlaceholder(string title, string subtitle)
        {
            return new Border
            {
                CornerRadius = new CornerRadius(16),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(18),
                Background = (Brush)FindResource("ContentCardBg"),
                BorderBrush = (Brush)FindResource("ContentStroke"),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = System.Windows.Media.Brushes.White },
                        new TextBlock { Text = subtitle, Margin = new Thickness(0,6,0,0), Foreground = (Brush)new BrushConverter().ConvertFromString("#9CA3AF") }
                    }
                }
            };
        }

        public async Task OpenAssetDetails(DiaQuotedAssetRow row)
        {
            if (row == null) return;
            MainViewModel mainViewModel = App.Services.GetRequiredService<MainViewModel>();

            _contentHistory.Push(MainContent.Content);
  
            var details = new QuotedAssetDetailsControl(mainViewModel._api);
            details.SetAsset(row);
            details.BackRequested += (_, __) => GoBack();

            MainContent.Content = details;
        }

        public void GoBack()
        {
            if (_contentHistory.Count == 0) return;
            MainContent.Content = _contentHistory.Pop();
        }

        private void OpenWatchlistWidget_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel mainViewModel = this.DataContext as MainViewModel;
            QuotedAssetsViewModel quotedAssetsView = mainViewModel.QuotedAssets;
            if (_widgetWin is { IsVisible: true })
            {
                _widgetWin.Activate();
                _widgetWin.Topmost = _widgetWin.Topmost; 
                return;
            }

            var wvm = new WatchlistWidgetViewModel(quotedAssetsView);
            
            _widgetWin = new WatchlistWidgetWindow(wvm)
            {
                Owner = Window.GetWindow(this)
            };
            
            _widgetWin.Show();
            _widgetWin.Activate();
        }
    }
}
