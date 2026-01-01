using DIAdataDesktop.Models;
using DIAdataDesktop.ViewModels;
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
using UserControl = System.Windows.Controls.UserControl;

namespace DIAdataDesktop.Views.Controls
{
    public partial class RwaControl : UserControl
    {
        public RwaControl()
        {
            InitializeComponent();
        }

        private async void FavoriteBtn_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (DataContext is not RwaViewModel vm) return;
            if (sender is not System.Windows.Controls.Button b) return;
            if (b.DataContext is not DiaRwaRow row) return;

            await vm.ToggleFavorite(row);
        }

        private void RwaIcon_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

        }

        private void RwaCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn) return;
            if (btn.DataContext is not DiaRwaRow row) return;

            if (!string.IsNullOrWhiteSpace(row.AppUrl))
            {
                Process.Start(new ProcessStartInfo(row.AppUrl) { UseShellExecute = true });
            }
        }
    }
}
