using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using WindowsCode.Studio.Models;
using WindowsCode.Studio.Services;
using WindowsCode.Studio.Views;
using WindowsCode.Studio.Views.Dialogs;
using WindowsCode.Studio.Views.TabViews;
using WinRT.Interop;
using WinUIEx;

namespace WindowsCode.Studio
{
    public sealed partial class MainWindow : WindowEx
    {
        private readonly AppWindow m_AppWindow;

        private SettingsModel settingsModel;

        public MainWindow()
        {
            InitializeComponent();

            PersistenceId = "MainWindow";

            App.SetMainWindow(this);

            EditorPage editor = new();

            ContentProvider.Navigate(editor.GetType());

            CheckWindowsVersion();

            GetCurrentUserAccount();

            AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/CodeStudioCanary.ico"));

            m_AppWindow = GetAppWindowForCurrentWindow();

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                AppWindowTitleBar titleBar = m_AppWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                AppTitleBar.Loaded += AppTitleBar_Loaded;
                AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
            }
            else
            {
                AppTitleBar.Visibility = Visibility.Collapsed;
            }

            Title = Windows.ApplicationModel.Package.Current.DisplayName;

            App.SetContentProvider(ContentProvider);

            CheckSettingsFile();
        }

        private void CheckWindowsVersion()
        {
            OperatingSystem WinOS = Environment.OSVersion;
            if (WinOS.Version.Build < 22000)
            {
                Backdrop = new AcrylicSystemBackdrop();
            }
            else if (WinOS.Version.Build >= 22000)
            {
                Backdrop = new MicaSystemBackdrop();
            }
        }

        private async void GetCurrentUserAccount()
        {
            IReadOnlyList<User> users = await User.FindAllAsync();

            User current = users.Where(p => p.AuthenticationStatus == UserAuthenticationStatus.LocallyAuthenticated &&
                                        p.Type == UserType.LocalUser).FirstOrDefault();

            // user may have username
            Windows.Foundation.IAsyncOperation<object> data = current.GetPropertyAsync(KnownUserProperties.DisplayName);
            string displayName = (string)await data;

            //or may be authinticated using hotmail 
            if (!string.IsNullOrEmpty(displayName))
            {

                string firstName = (string)await current.GetPropertyAsync(KnownUserProperties.FirstName);
                string lastName = (string)await current.GetPropertyAsync(KnownUserProperties.LastName);
                string email = (string)await current.GetPropertyAsync(KnownUserProperties.AccountName);
                StorageFile image = await current.GetPictureAsync(UserPictureSize.Size64x64) as StorageFile;
                if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName) || !string.IsNullOrWhiteSpace(email))
                {
                    if (image != null)
                    {
                        try
                        {
                            IRandomAccessStream imgStrm = await image.OpenReadAsync();
                            BitmapImage bitImg = new();
                            bitImg.SetSource(imgStrm);
                            AccountPicture.ProfilePicture = bitImg;
                        }
                        catch (Exception ex)
                        {
                            ContentDialog errorDialog = new()
                            {
                                Title = "Error while getting Profile Picture",
                                Content = ex.Message,
                                CloseButtonText = "OK",
                                DefaultButton = ContentDialogButton.Close
                            };
                            errorDialog.XamlRoot = Content.XamlRoot;
                            await errorDialog.ShowAsync();
                        }
                    }
                    else
                    {
                        AccountPicture.Initials = firstName[0].ToString() + lastName[0].ToString();
                    }
                    displayName = string.Format("{0} {1}", firstName, lastName);
                    AccountEmail.Text = email;
                    AccountName.Text = displayName;
                }
                else
                {
                    firstName = string.Empty;
                    lastName = string.Empty;
                    email = string.Empty;
                }
            }
            else
            {
                displayName = string.Empty;
            }
        }

        private ElementTheme requestedTheme = ElementTheme.Default;
        private ApplicationTheme actualTheme = ApplicationTheme.Dark;

        public ElementTheme RequestedTheme
        {
            get => requestedTheme;
            set
            {
                requestedTheme = value;

                switch (value)
                {
                    case ElementTheme.Dark: actualTheme = ApplicationTheme.Dark; break;
                    case ElementTheme.Light: actualTheme = ApplicationTheme.Light; break;
                    case ElementTheme.Default:
                        UISettings uiSettings = new();
                        Color defaultthemecolor = uiSettings.GetColorValue(UIColorType.Background);
                        actualTheme = defaultthemecolor == Colors.Black ? ApplicationTheme.Dark : ApplicationTheme.Light;
                        break;
                }
            }
        }

        private async void CheckSettingsFile()
        {
            settingsModel = await SettingsService.ReadSettingsFileAsync(Content);

            if (settingsModel == null)
            {
                // If the settings file doesn't exist, create a new settings model
                settingsModel = new SettingsModel();

                // Save the newly created settings to the file
                new SettingsService().CreateSettingsFileJson(Content);

                // You might want to consider awaiting the file creation before proceeding
                // await new SettingsService().CreateSettingsFileJsonAsync(settingsModel);

                // Optionally, you can re-read the settings file to ensure it was created and loaded correctly
                settingsModel = await SettingsService.ReadSettingsFileAsync(Content);
            }

            // Now you have either loaded settings from the file or created new ones
            // Do further processing as needed
            AppInfoReporter.IsOpen = settingsModel.IsConfidentialInfoBarEnabled;

            if (settingsModel.AppTheme != SettingsModel.Theme.Default)
            {
                //RequestedTheme = settingsModel.AppTheme != SettingsModel.Theme.Light ? ElementTheme.Dark : ElementTheme.Light;
            }
            else
            {
                UISettings uISettings = new();
                uISettings.ColorValuesChanged += (sender, args) =>
                {
                    Color systemBackgroundColor = sender.GetColorValue(UIColorType.Background);

                    bool isDarkTheme = systemBackgroundColor.R + systemBackgroundColor.G + systemBackgroundColor.B < 382;

                    Application.Current.RequestedTheme = isDarkTheme ? ApplicationTheme.Dark : ApplicationTheme.Light;
                };
            }
        }

        #region WinPTR
        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                SetDragRegionForCustomTitleBar(m_AppWindow);
            }
        }
        private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AppWindowTitleBar.IsCustomizationSupported()
        && m_AppWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                SetDragRegionForCustomTitleBar(m_AppWindow);
            }
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            Microsoft.UI.WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);
        internal enum Monitor_DPI_Type : int
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }

        private double GetScaleAdjustment()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            Microsoft.UI.WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
            IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

            // Get DPI.
            int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
            if (result != 0)
            {
                throw new Exception("Could not get DPI for monitor.");
            }

            uint scaleFactorPercent = (uint)((((long)dpiX * 100) + (96 >> 1)) / 96);
            return scaleFactorPercent / 100.0;
        }

        private void SetDragRegionForCustomTitleBar(AppWindow appWindow)
        {
            if (AppWindowTitleBar.IsCustomizationSupported() && appWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                double scaleAdjustment = GetScaleAdjustment();

                RightPaddingColumn.Width = new GridLength(appWindow.TitleBar.RightInset / scaleAdjustment);
                LeftPaddingColumn.Width = new GridLength(appWindow.TitleBar.LeftInset / scaleAdjustment);

                List<Windows.Graphics.RectInt32> dragRectsList = new();

                Windows.Graphics.RectInt32 dragRectL;
                dragRectL.X = (int)(LeftPaddingColumn.ActualWidth * scaleAdjustment);
                dragRectL.Y = 0;
                dragRectL.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
                dragRectL.Width = (int)((IconColumn.ActualWidth
                                        + TitleColumn.ActualWidth
                                        + LeftDragColumn.ActualWidth) * scaleAdjustment);
                dragRectsList.Add(dragRectL);

                Windows.Graphics.RectInt32 dragRectR;
                dragRectR.X = (int)((LeftPaddingColumn.ActualWidth
                                    + IconColumn.ActualWidth
                                    + TitleTextBlock.ActualWidth
                                    + LeftDragColumn.ActualWidth
                                    + SearchColumn.ActualWidth) * scaleAdjustment);
                dragRectR.Y = 0;
                dragRectR.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
                dragRectR.Width = (int)(RightDragColumn.ActualWidth * scaleAdjustment);
                dragRectsList.Add(dragRectR);

                Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

                appWindow.TitleBar.SetDragRectangles(dragRects);
            }
        }

        #endregion

        private async void UpdateItem_Click(object sender, RoutedEventArgs e)
        {
            await new UpdateService().GetUpdates(Content);
        }

        private async void AboutItem_Click(object sender, RoutedEventArgs e)
        {
            await new AboutDialog() { XamlRoot = Content.XamlRoot }.ShowAsync();
        }

        private void ShowWelcomePage_Click(object sender, RoutedEventArgs e)
        {
            TabService tabService = new(App.GetEditorPage().FileTabView);

            tabService.CreateTabItem("Welcome", new WelcomePage());
            tabService.GetTabView().SelectedIndex += 1;
        }

        private async void NewFile_Click(object sender, RoutedEventArgs e)
        {
            New_File newFileDialog = new() { XamlRoot = Content.XamlRoot };

            ContentDialogResult dialogResult = await newFileDialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary && !string.IsNullOrEmpty(newFileDialog.GetFileName()))
            {
                TabService tabService = new(App.GetEditorPage().FileTabView);
                tabService.CreateTabItem($"{newFileDialog.GetFileName()}", new EditBoxTabView());
                tabService.GetTabView().SelectedIndex += 1;
            }
        }

        private void AppOptionButton_Click(object sender, RoutedEventArgs e)
        {
            TabService tabService = new(App.GetEditorPage().FileTabView);
            tabService.CreateTabItem("Settings", new SettingsPage());
            tabService.GetTabView().SelectedIndex += 1;
        }

        private async void WhatsNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TabService tabService = new(App.GetEditorPage().FileTabView);
            TabViewItem whatsNewTab = await tabService.CreateTabItemAsync("What's New", new WhatsNewPage(), new FontIconSource()
            {
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                Glyph = "&#xE789;"
            });
            tabService.GetTabHeader(whatsNewTab);
            tabService.GetTabView().SelectedIndex = tabService.GetTabView().TabItems.IndexOf(whatsNewTab);
        }
    }
}
