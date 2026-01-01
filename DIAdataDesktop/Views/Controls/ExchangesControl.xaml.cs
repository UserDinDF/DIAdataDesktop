using DIAdataDesktop.Models;
using DIAdataDesktop.ViewModels;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
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
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace DIAdataDesktop.Views.Controls
{
    public partial class ExchangesControl : System.Windows.Controls.UserControl
    {
        public ExchangesControl()
        {
            InitializeComponent();
        }

        private void SvgLogo_Loaded(object sender, RoutedEventArgs e)
        {

            if (sender is not System.Windows.Controls.Image img) return;
            if (img.DataContext is not DiaExchange ex) return;
            if (ex.LogoSvgPath == null)
            {
                return;
            }

            try
            {
                var settings = new WpfDrawingSettings
                {
                    IncludeRuntime = false,
                    TextAsGeometry = false
                };

                var reader = new FileSvgReader(settings);

                using Stream stream = Application.GetResourceStream(ex.LogoSvgPath)!.Stream;

                DrawingGroup drawing = reader.Read(stream);
                if (drawing != null)
                {
                    img.Source = new DrawingImage(drawing);
                }
            }
            catch
            {
            }
        }

        private void OpenExchangeSource_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.DataContext is not DiaExchange ex) return;

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = $"https://www.diadata.org/app/source/exchange/{ex.Name}/",
                UseShellExecute = true
            });

        }

        private async void FavoriteBtn_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; 

            if (DataContext is not ExchangesViewModel vm) return;

            if (sender is Button b && b.DataContext is DiaExchange row)
            {
                await vm.ToggleFavorite(row);
            }
        }
    }
}
