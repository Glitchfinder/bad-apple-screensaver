using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace BadAppleScr2
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>

    public partial class ScreenSaverWindow : Window
    {
        public bool isPreview = false;
        private static readonly ShaderEffect fx = new GrayscaleEffect
        {
            Chrominance = App.Config.Chrominance,
            Negative = App.Config.Negative,
            LeaveBlack = App.Config.LeaveBlack
        };

        public ScreenSaverWindow(VisualBrush brush)
        {
            InitializeComponent();

            //load settings
            brush.Stretch = App.Config.Stretch;
            this.VideoBlock.Fill = brush;
            VideoBlock.Visibility = System.Windows.Visibility.Visible;
        }

        void ScreenSaverWindow_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            ShowInTaskbar = true;
            Topmost = false;
#else
            // Add WS_EX_TOOLWINDOW window style
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            int exStyle = (int)NativeMethods.GetWindowLong(wndHelper.Handle, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE);
            exStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLong(wndHelper.Handle, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

            if (!isPreview)
            {
                Mouse.OverrideCursor = Cursors.None;
            }
#endif
        }

        void ScreenSaverWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!isPreview)
                Application.Current.Shutdown();
        }

        void ScreenSaverWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (!isPreview)
                Application.Current.Shutdown();
        }

        void ScreenSaverWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isPreview)
                Application.Current.Shutdown();
        }

        bool isActive;
        Point mousePosition;

        void ScreenSaverWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPreview)
                return;
            Point currentPosition = e.MouseDevice.GetPosition(this);
            // Set IsActive and MouseLocation only the first time this event is called.
            if (!isActive)
            {
                mousePosition = currentPosition;
                isActive = true;
            }
            else
            {
                // If the mouse has moved significantly since first call, close.
                if ((Math.Abs(mousePosition.X - currentPosition.X) > 10) ||
                    (Math.Abs(mousePosition.Y - currentPosition.Y) > 10))
                {
                    Application.Current.Shutdown();
                }
            }
        }
    }
}
