using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DIAdataDesktop.ViewModels
{
    public partial class TokenDetailsPopUpViewModel : ObservableObject
    {
        public DiaQuotedAssetRow Row { get; }

        // UI toggles
        [ObservableProperty] private bool isTopmost;
        [ObservableProperty] private bool showPrice = true;
        [ObservableProperty] private bool showVolume = true;
        [ObservableProperty] private bool showSources = true;
        [ObservableProperty] private bool showNetwork = true;
        [ObservableProperty] private bool showUpdated = true;
        [ObservableProperty] private bool showPriceYesterday = true;

        public TokenDetailsPopUpViewModel(DiaQuotedAssetRow row)
        {
            Row = row ?? throw new ArgumentNullException(nameof(row));
        }

        [RelayCommand]
        private void OpenBrowser()
        {
            var network = Uri.EscapeDataString(Row.Blockchain ?? "");
            var address = Row.Address ?? "";

            if (string.IsNullOrWhiteSpace(network) || string.IsNullOrWhiteSpace(address))
                return;

            var url = $"https://www.diadata.org/app/price/asset/{network}/{address}/";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}