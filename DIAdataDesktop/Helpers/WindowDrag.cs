using System.Windows;
using System.Windows.Input;

namespace DIAdataDesktop.Helpers
{
    public static class WindowDrag
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(WindowDrag),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement el) return;

            if ((bool)e.NewValue)
                el.PreviewMouseLeftButtonDown += OnMouseDown;
            else
                el.PreviewMouseLeftButtonDown -= OnMouseDown;
        }

        private static void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed) return;

            var dep = sender as DependencyObject;
            var win = Window.GetWindow(dep);
            if (win == null) return;

            // Double click = maximize/restore
            if (e.ClickCount == 2 && win.ResizeMode != ResizeMode.NoResize)
            {
                win.WindowState = win.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                return;
            }

            win.DragMove();
        }
    }
}
