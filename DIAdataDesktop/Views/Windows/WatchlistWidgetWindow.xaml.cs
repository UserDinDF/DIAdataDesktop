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
using System.Windows.Shapes;

namespace DIAdataDesktop.Views
{
    public partial class WatchlistWidgetWindow : Window
    {
        public WatchlistWidgetWindow()
        {
            InitializeComponent();
        }

        public WatchlistWidgetWindow(object vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
