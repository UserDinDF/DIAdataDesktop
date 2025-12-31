using DIAdataDesktop.Models;
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

namespace DIAdataDesktop.Views.Controls
{
    /// <summary>
    /// Interaktionslogik für ExchangesControl.xaml
    /// </summary>
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
                // WPF renderer settings
                var settings = new WpfDrawingSettings
                {
                    IncludeRuntime = false,
                    TextAsGeometry = false
                };

                var reader = new FileSvgReader(settings);

                // Wenn du pack:// URIs nutzt, brauchst du Stream:
                using Stream stream = Application.GetResourceStream(ex.LogoSvgPath)!.Stream;

                DrawingGroup drawing = reader.Read(stream);
                if (drawing != null)
                {
                    img.Source = new DrawingImage(drawing);
                }
            }
            catch
            {
                // optional fallback
            }
        }
    }
}
