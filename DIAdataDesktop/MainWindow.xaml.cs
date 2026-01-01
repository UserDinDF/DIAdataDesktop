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

            NavigateTo("Quotation");
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
                "Quotation" => BuildQuotationPage(),
                "QuotedAssets" => await BuildQuotedAssetsPage(),
                "Exchanges" => BuildExchangesPage(),

                "DataLibrary" => BuildPlaceholder("Data library", "Choose a module on the left."),

                "DigitalAssets" => await BuildQuotedAssetsPage(),
                "RealWorldAssets" => BuildPlaceholder("Real-world assets", "Coming soon..."),
                "Randomness" => BuildPlaceholder("Randomness", "Coming soon..."),
                "OraclePlayground" => BuildPlaceholder("Oracle playground", "Coming soon..."),
                "DataSources" => BuildPlaceholder("Data sources", "Coming soon..."),
                "SupportedChains" => BuildPlaceholder("Supported chains", "Coming soon..."),
                "Staking" => BuildPlaceholder("Staking", "Coming soon..."),
                "Bridge" => BuildPlaceholder("Bridge", "Coming soon..."),
                _ => BuildPlaceholder("Unknown", key)
            };

            _pageCache[key] = page;
            MainContent.Content = page;
        }

        private UIElement BuildExchangesPage()
        {
            var ctrl = App.Services.GetRequiredService<ExchangesControl>();
            ctrl.DataContext = _vm.ExchangesVm;
            return ctrl;
        }

        private UIElement BuildQuotationPage()
        {
            var ctrl = App.Services.GetRequiredService<QuotationControl>();
            ctrl.DataContext = _vm.Quotation;  
            return ctrl;
        }

        private async Task<UIElement> BuildQuotedAssetsPage()
        {
            var ctrl = App.Services.GetRequiredService<QuotedAssetsControl>();
            ctrl.DataContext = _vm.QuotedAssets;
            await _vm.QuotedAssets.InitializeAsync();
            return ctrl;
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
            // aktuelle Seite merken (damit Back exakt zurück geht)
            _contentHistory.Push(MainContent.Content);
           var alleExchanges = mainViewModel.ExchangesVm._all;
            var pairsAssetCex = await mainViewModel._api.GetPairsAssetCexAsync(row.Blockchain, row.Address);
            foreach (var exchange in alleExchanges)
            {
                //var pairCex = await mainViewModel._api.GetPairsCexAsync(exchange.Name);
                
                //foreach (var item in pairCex)
                //{
                //    //if (item.Symbol == row.Symbol)
                //    //{
                //    //    var test = await mainViewModel._api.GetQuotationByAddressAsync(item.UnderlyingPair.BaseToken.Blockchain, item.UnderlyingPair.BaseToken.Address);
                //    //}
                //}
                //var addesQuotations = new List<DiaQuotedAsset>();

                //addesQuotations = await mainViewModel._api.GetQuotedAssetsAsync(row.Blockchain);
                //double addedSUm = 0;
                //foreach (var item in pairsAssetCex)
                //{
                //    if (item.Symbol == row.Symbol)
                //    {

                //        var test2 = await mainViewModel._api.GetPairsAssetCexAsync(item.UnderlyingPair.BaseToken.Blockchain, item.UnderlyingPair.BaseToken.Address);

                //        //foreach (var added in test2)
                //        //{
                //        //    if (added.Symbol == row.Symbol)
                //        //    {
                //        //        var quotation = await mainViewModel._api.GetQuotationByAddressAsync(added.UnderlyingPair.QuoteToken.Blockchain, added.UnderlyingPair.QuoteToken.Address);
                //        //        addedSUm += quotation.VolumeYesterdayUSD;

                //        //    }

                //        //}
                //    }
                }
            


            //var sym = (row.Symbol ?? "").Trim();
            //var totalMoney = addesQuotations
            //    .Where(q =>
            //        string.Equals(
            //            (q?.Asset?.Symbol ?? "").Trim(),
            //            sym,
            //            StringComparison.OrdinalIgnoreCase))
            //    .Sum(q => q?.Volume ?? 0d);


            foreach (var item in alleExchanges)
            {

            }
            var details =new QuotedAssetDetailsControl(mainViewModel._api);
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
