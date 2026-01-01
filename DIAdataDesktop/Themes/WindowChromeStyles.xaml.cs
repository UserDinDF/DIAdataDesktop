using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace DIAdataDesktop.Themes
{
    public partial class WindowChromeStyles : ResourceDictionary
    {
        public WindowChromeStyles()
        {
            InitializeComponent();
        }


        private void Minimize_Click(object sender, RoutedEventArgs e) 
        {
            var window = (Window)((FrameworkElement)sender).TemplatedParent;
            window.WindowState = System.Windows.WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            var window = (Window)((FrameworkElement)sender).TemplatedParent;
            if (window.WindowState == System.Windows.WindowState.Normal)
            {
                window.WindowState = System.Windows.WindowState.Maximized;
            }
            else
            {
                window.WindowState = System.Windows.WindowState.Normal;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var window = (Window)((FrameworkElement)sender).TemplatedParent;
            window.Close();
        }

        private void Button_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var window = (Window)((FrameworkElement)sender).TemplatedParent;
            window.WindowState = System.Windows.WindowState.Minimized;
        }

        private void Button_PreviewMinimizeMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {


        }

        private void Button_PreviewMaximizeMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        private void Button_PreviewCloseMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }
    }
}
