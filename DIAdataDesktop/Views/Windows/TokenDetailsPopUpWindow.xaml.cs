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

namespace DIAdataDesktop.Views.Windows
{
    public partial class TokenDetailsPopUpWindow : Window
    {
        public TokenDetailsPopUpWindow()
        {
            InitializeComponent();
        }

        public TokenDetailsPopUpWindow(object vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void TopmostCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        private void TopmostCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }
    }
}
