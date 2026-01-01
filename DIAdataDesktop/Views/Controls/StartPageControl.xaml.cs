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

namespace DIAdataDesktop.Views.Controls
{
    public partial class StartPageControl : System.Windows.Controls.UserControl
    {
        public StartPageControl()
        {
            InitializeComponent();
        }

        private void SvgLogo_Loaded(object sender, RoutedEventArgs e)
        {
            StartPageViewModel startPageViewModel = DataContext as StartPageViewModel;
            MainViewModel mainViewModel = Application.Current.MainWindow.DataContext as MainViewModel;

            if (sender is not System.Windows.Controls.Image img) return;
            if (img.DataContext is not FavoriteTileVM ex) return;

            var searchedItem = mainViewModel.ExchangesVm._all.Where(x => x.Name == ex.Title).FirstOrDefault();

            if (searchedItem.LogoSvgPath == null)
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

                using Stream stream = Application.GetResourceStream(searchedItem.LogoSvgPath)!.Stream;

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
    }
}
