using DIAdataDesktop.Models;
using DIAdataDesktop.ViewModels;
using DIAdataDesktop.Views.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Button = System.Windows.Controls.Button;

namespace DIAdataDesktop.Views.Controls
{
    public partial class QuotedAssetsControl : System.Windows.Controls.UserControl
    {
        public QuotedAssetsControl()
        {
            InitializeComponent();
        }
        
        private async void TokenCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            MainViewModel viewModel = App.Current.MainWindow?.DataContext as MainViewModel;

            if (btn.DataContext is DiaQuotedAssetRow row)
            {
                //var exchangeByAsset = await viewModel._api.GetPairsAssetCexAsync(row.Blockchain, row.Address);
                //row.CexPairs = new System.Collections.ObjectModel.ObservableCollection<DiaCexPairsByAssetRow>(exchangeByAsset);

                if (Window.GetWindow(this) is MainWindow mw)
                {
                    await mw.OpenAssetDetails(row);
                }
            }

        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

        }

        private void openInWindowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.DataContext is not DiaQuotedAssetRow row) return;

            var vm = new TokenDetailsPopUpViewModel(row);

            var win = new TokenDetailsPopUpWindow(vm)
            {
                Owner = Window.GetWindow(this)
            };

            win.Show();
            win.Activate();
        }

        private async void FavoriteBtn_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // verhindert TokenCard_Click

            if (DataContext is not QuotedAssetsViewModel vm) return;

            if (sender is Button b && b.DataContext is DiaQuotedAssetRow row)
            {
                await vm.ToggleFavorite(row);
            }
        }
    }
}
