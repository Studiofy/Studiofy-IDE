using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Studiofy.IDE.Pages;
using Studiofy.IDE.Pages.NavigationViewPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Studiofy.IDE
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly AppWindow m_AppWindow;

        #region Window Initialization

        public MainWindow()
        {
            InitializeComponent();

            SystemBackdrop = new MicaBackdrop();
            m_AppWindow = GetAppWindowForCurrentView();
            m_AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/StudiofyIDECanaryLogo.ico"));

            if (AppWindowTitleBar.IsCustomizationSupported() is true)
            {
                AppWindowTitleBar wndTitleBar = m_AppWindow.TitleBar;
                wndTitleBar.ExtendsContentIntoTitleBar = true;
                wndTitleBar.ButtonBackgroundColor = Colors.Transparent;
                wndTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                wndTitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
                CustomWindowTitleBar.Loaded += CustomWindowTitleBar_Loaded;
                CustomWindowTitleBar.SizeChanged += CustomWindowTitleBar_SizeChanged;
            }
            else
            {
                CustomWindowTitleBar.Visibility = Visibility.Collapsed;
            }

            Title = Windows.ApplicationModel.Package.Current.DisplayName;

            AppNavigationView.SelectedItem = ExplorerMenuItem;
            AppContentFrame.Navigate(typeof(ExplorerNavViewPage));

            EditorTabView.TabItems.Add(new TabViewItem()
            {
                Header = "Welcome",
                Content = new MainPage(),
                IconSource = new SymbolIconSource()
                {
                    Symbol = Symbol.Document
                }
            });
        }

        internal enum Monitor_DPI_Type : int
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }

        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

        private void CustomWindowTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AppWindowTitleBar.IsCustomizationSupported() is true && m_AppWindow.TitleBar.ExtendsContentIntoTitleBar is true)
            {
                SetDragRegionForCustomTitleBar(m_AppWindow);
            }
        }

        private void CustomWindowTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppWindowTitleBar.IsCustomizationSupported() is true)
            {
                SetDragRegionForCustomTitleBar(m_AppWindow);
            }
        }

        private AppWindow GetAppWindowForCurrentView()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        private void SetDragRegionForCustomTitleBar(AppWindow m_AppWindow)
        {
            if (AppWindowTitleBar.IsCustomizationSupported() is true
            && m_AppWindow.TitleBar.ExtendsContentIntoTitleBar is true)
            {
                double scaleAdjustment = GetScaleAdjustment();

                RightPaddingColumn.Width = new GridLength(m_AppWindow.TitleBar.RightInset / scaleAdjustment);
                LeftPaddingColumn.Width = new GridLength(m_AppWindow.TitleBar.LeftInset / scaleAdjustment);

                List<Windows.Graphics.RectInt32> dragRectsList = new();

                Windows.Graphics.RectInt32 dragRectL;
                dragRectL.X = (int)((LeftPaddingColumn.ActualWidth) * scaleAdjustment);
                dragRectL.Y = 0;
                dragRectL.Height = (int)(CustomWindowTitleBar.ActualHeight * scaleAdjustment);
                dragRectL.Width = (int)((IconColumn.ActualWidth
                                        + LeftDragColumn.ActualWidth) * scaleAdjustment);
                dragRectsList.Add(dragRectL);

                Windows.Graphics.RectInt32 dragRectR;
                dragRectR.X = (int)((LeftPaddingColumn.ActualWidth
                                    + IconColumn.ActualWidth
                                    + LeftDragColumn.ActualWidth
                                    + MenuBarColumn.ActualWidth) * scaleAdjustment);
                dragRectR.Y = 0;
                dragRectR.Height = (int)(CustomWindowTitleBar.ActualHeight * scaleAdjustment);
                dragRectR.Width = (int)(RightDragColumn.ActualWidth * scaleAdjustment);
                dragRectsList.Add(dragRectR);

                Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

                m_AppWindow.TitleBar.SetDragRectangles(dragRects);
            }
        }
        private double GetScaleAdjustment()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
            IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

            // Get DPI.
            int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
            if (result != 0)
            {
                throw new Exception("Could not get DPI for monitor.");
            }

            uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
            return scaleFactorPercent / 100.0;
        }

        #endregion

        private void AppNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (sender.SelectedItem as NavigationViewItem == ExplorerMenuItem)
            {
                AppContentFrame.Navigate(typeof(ExplorerNavViewPage));
            }
            else if (sender.SelectedItem as NavigationViewItem == ExtensionMenuItem)
            {
                AppContentFrame.Navigate(typeof(ExtensionsNavViewPage));
            }
        }
    }
}
