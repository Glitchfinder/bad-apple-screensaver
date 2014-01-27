using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace BadAppleScr2
{
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>

    public partial class App : Application
    {
        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

        internal static Config Config = Config.Open(string.Format("{0}{1}BadAppleScr{1}config.json", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), System.IO.Path.DirectorySeparatorChar));
        internal static Regex re_arg = new Regex(@"/([CPS])(?:[\s:](\d+))?", RegexOptions.Compiled);
        private HwndSource winWPFContent;
        private ScreenSaverWindow winSaver;
        private MediaElement VideoElement;
        private ShaderEffect fx;

        internal void BadAppleScr2_Startup(object sender, StartupEventArgs e)
        {
            ProcessCommandLineArgs(e.Args);
        }

        internal void ProcessCommandLineArgs(string[] args)
        {
            if (args.Length > 0)
            {
                // Parse command line argument 
                Match match = re_arg.Match(args[0].ToUpperInvariant().Trim());
                if (match.Success)
                {
                    string action = match.Groups[1].Value;
                    //IntPtr hwnd = (match.Groups[2].Value.Length > 0) ? (IntPtr)int.Parse(match.Groups[2].Value) : IntPtr.Zero;
                    switch (action)
                    {
                        case "C":
                            // Show the options dialog
                            ShowConfig();
                            break;
                        case "P":
                            if (args.Length == 1)
                            {
                                // Don't do anything for preview
                                Application.Current.Shutdown();
                                break;
                            }
                            ShowPreview(args[1]);
                            break;
                        case "S":
                            // Show screensaver form
                            ShowScreensaver();
                            break;
                        default:
                            // If invalid arguments were passed in, show the screensaver 
                            ShowScreensaver();
                            break;
                    }
                }
            }
            else
            {
                // If no arguments were passed in, show the screensaver 
                ShowScreensaver();
            }
        }

        /// <summary>
        /// Shows screen saver preview by creating one instance of ScreenSaverWindow.
        /// 
        /// Note: uses WinForms's Screen class to get monitor info.
        /// </summary>
        internal void ShowPreview(String arg)
        {
            System.Windows.Point scale = Interop.GetVisualScale();
            Debug.WriteLine(scale);

            fx = new GrayscaleEffect
            {
                Chrominance = App.Config.Chrominance,
                Negative = App.Config.Negative,
                LeaveBlack = App.Config.LeaveBlack
            };

            //set volume and stretch method
            this.VideoElement             = new MediaElement();
            VideoElement.UnloadedBehavior = MediaState.Manual;
            VideoElement.Source           = App.Config.Video;
            VideoElement.IsMuted          = false;
            VideoElement.Volume           = App.Config.Volume;
            VideoElement.Stretch          = App.Config.Stretch;
            VideoElement.MediaEnded  += new RoutedEventHandler(VideoElement_MediaEnded);
            VideoElement.MediaOpened += new RoutedEventHandler(VideoElement_MediaOpened);

            Int32  previewHandle = Convert.ToInt32(arg);
            IntPtr pPreviewHnd   = new IntPtr(previewHandle);
            RECT lpRect          = new RECT();
            bool bGetRect        = GetClientRect(pPreviewHnd, ref lpRect);

            this.winSaver = new ScreenSaverWindow(new VisualBrush(VideoElement));
            HwndSourceParameters sourceParams = new HwndSourceParameters("sourceParams");

            //set window properties
            sourceParams.Height = lpRect.Bottom - lpRect.Top;
            sourceParams.Width = lpRect.Right - lpRect.Left;
            sourceParams.ParentWindow = pPreviewHnd;
            sourceParams.WindowStyle = (int)(0x10000000 | 0x40000000 | 0x02000000);

            //set up preview
            winWPFContent = new HwndSource(sourceParams);
            winWPFContent.Disposed += new EventHandler(winWPFContent_Disposed);
            winWPFContent.RootVisual   = winSaver.grid1;
            winSaver.Width             = (lpRect.Right  - lpRect.Left) / scale.X;
            winSaver.Height            = (lpRect.Bottom - lpRect.Top)  / scale.Y;
            winSaver.VideoBlock.Width  = (lpRect.Right  - lpRect.Left) / scale.X;
            winSaver.VideoBlock.Height = (lpRect.Right  - lpRect.Left) / scale.X;

            //set a background
            //Note: Uses an arbitrary screen size to determine when to do this
            if (winSaver.Width <= 320 && winSaver.Height <= 240)
            {
                winSaver.DesktopBackground.Source = new BitmapImage(new Uri(Interop.GetDesktopWallpaper(), UriKind.Absolute));
                winSaver.DesktopBackground.Stretch = Interop.GetWallpaperStretch();
            }

            winSaver.isPreview = true;
            winSaver.Show();
            VideoElement.Play();
        }

        /// <summary>
        /// Shows screen saver by creating one instance of ScreenSaverWindow for each monitor.
        /// 
        /// Note: uses WinForms's Screen class to get monitor info.
        /// </summary>
        internal void ShowScreensaver()
        {
            System.Windows.Point scale = Interop.GetVisualScale();
            Debug.WriteLine(scale);

            fx = new GrayscaleEffect
            {
                Chrominance = App.Config.Chrominance,
                Negative = App.Config.Negative,
                LeaveBlack = App.Config.LeaveBlack
            };

            //System.Windows.Forms.Screen primary = System.Windows.Forms.Screen.PrimaryScreen;

            //set volume and stretch method
            this.VideoElement             = new MediaElement();
            VideoElement.UnloadedBehavior = MediaState.Manual;
            VideoElement.Source           = App.Config.Video;
            VideoElement.IsMuted          = false;
            VideoElement.Volume           = App.Config.Volume;
            VideoElement.Stretch          = App.Config.Stretch;
            VideoElement.MediaEnded  += new RoutedEventHandler(VideoElement_MediaEnded);
            VideoElement.MediaOpened += new RoutedEventHandler(VideoElement_MediaOpened);

            //creates window on every screens
            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                ScreenSaverWindow window     = new ScreenSaverWindow(new VisualBrush(VideoElement));
                window.WindowStartupLocation = WindowStartupLocation.Manual;

                //covers entire monitor
                Debug.WriteLine(screen.Bounds);
                window.Left              = screen.Bounds.Left / scale.X;
                window.Top               = screen.Bounds.Top / scale.Y;
                window.Width             = (screen.Bounds.Right  - screen.Bounds.Left) / scale.X;
                window.Height            = (screen.Bounds.Bottom - screen.Bounds.Top)  / scale.Y;
                window.VideoBlock.Width  = window.Width;
                window.VideoBlock.Height = window.Height;
                window.Show();
            }

            VideoElement.Play();
        }

        internal void ShowConfig()
        {
            ConfigWindow window = new ConfigWindow();
            window.ShowDialog();
        }

        void winWPFContent_Disposed(object sender, EventArgs e)
        {
            winSaver.Close();
            Application.Current.Shutdown();
        }

        void VideoElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            VideoElement.Position = TimeSpan.Zero;
            VideoElement.Play();
        }

        void VideoElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            VideoElement.Effect = fx;
        }
    }
}
