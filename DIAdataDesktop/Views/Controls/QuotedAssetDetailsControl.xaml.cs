using DIAdataDesktop.Models;
using DIAdataDesktop.Services;
using System;
using System.Collections.Generic;
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

namespace DIAdataDesktop.Views.Controls
{
    public partial class QuotedAssetDetailsControl : System.Windows.Controls.UserControl
    {
        private readonly DiaApiClient _api;

        public event EventHandler? BackRequested;

        public string? ErrorText { get; private set; }
        public Visibility ErrorVisibility => string.IsNullOrWhiteSpace(ErrorText) ? Visibility.Collapsed : Visibility.Visible;

        public QuotedAssetDetailsControl(DiaApiClient api)
        {
            InitializeComponent();
            _api = api;
        }

        public void SetAsset(DiaQuotedAssetRow row)
        {
            DataContext = row;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
            => BackRequested?.Invoke(this, EventArgs.Empty);

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DiaQuotedAssetRow row) return;

            try
            {
                ErrorText = null;
                InvalidateVisual();

                var sym = row.Symbol;
                if (string.IsNullOrWhiteSpace(sym)) return;

                var quote = await _api.GetQuotationBySymbolAsync(sym, CancellationToken.None);
                row.UpdateQuotation(quote);

                var pairs = await _api.GetPairsAssetCexAsync(quote.Blockchain ?? "", quote.Address ?? "", ct: CancellationToken.None);

                row.CexPairs.Clear();
                foreach (var p in pairs)
                    row.CexPairs.Add(p);

                row.RecalcCexCounts();
            }
            catch (Exception ex)
            {
                ErrorText = ex.Message;
            }
            finally
            {
                InvalidateVisual();
            }
        }
    }
}
