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
        private void TokenCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn) return;
            if (btn.Tag is not string tag) return;

            // Tag: "Network|Name|Address"
            var parts = tag.Split('|');
            if (parts.Length < 3) return;

            var network = Uri.EscapeDataString(parts[0] ?? "");
            var name = Uri.EscapeDataString(parts[1] ?? "");
            var address = parts[2] ?? "";

            if (string.IsNullOrWhiteSpace(network) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
                return;

            var url = $"https://www.diadata.org/app/price/asset/{network}/{address}/";

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Open link failed", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}
