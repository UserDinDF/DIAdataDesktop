using DIAdataDesktop.ViewModels;
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

        // ✅ Cache: pro Key genau 1 UIElement (State bleibt)
        private readonly Dictionary<string, UIElement> _pageCache = new();

        public MainWindow()
        {
            InitializeComponent();

            _vm = App.Services.GetRequiredService<MainViewModel>();
            DataContext = _vm;

            // Default: z.B. Quotation
            NavigateTo("Quotation");
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb && tb.Tag is string key)
            {
                _vm.SelectedNav = key;   // ✅ UI highlight
                NavigateTo(key);
            }
        }

        private async void NavigateTo(string key)
        {
            //if (_pageCache.TryGetValue(key, out var cached))
            //{
            //    MainContent.Content = cached;
            //}

            UIElement page = key switch
            {
                // ✅ getrennt
                "Quotation" => BuildQuotationPage(),
                "QuotedAssets" => await BuildQuotedAssetsPage(),

                // optional: DataLibrary Overview
                "DataLibrary" => BuildPlaceholder("Data library", "Choose a module on the left."),

                // rest
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


        private UIElement BuildQuotationPage()
        {
            var ctrl = App.Services.GetRequiredService<QuotationControl>();
            ctrl.DataContext = _vm.Quotation;   // ✅ Child VM aus MainVM
            return ctrl;
        }

        private async Task<UIElement> BuildQuotedAssetsPage()
        {
            var ctrl = App.Services.GetRequiredService<QuotedAssetsControl>();
            ctrl.DataContext = _vm.QuotedAssets; // ✅ Child VM aus MainVM
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
    }
}
