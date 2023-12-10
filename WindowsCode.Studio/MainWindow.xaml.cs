using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using WindowsCode.Studio.Views;
using WinRT.Interop;
using WinUIEx;

namespace WindowsCode.Studio
{
    public sealed partial class MainWindow : WindowEx
    {
        private readonly AppWindow m_AppWindow;
        public static MainWindow activeWindow;

        public MainWindow()
        {
            activeWindow = this;
            PersistenceId = "MainWindow";
            InitializeComponent();
            CheckWindowsVersion();
            GetCurrentUserAccount();
            WelcomePage welcome = new();
            _ = ContentProvider.Navigate(welcome.GetType());

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
                // Show alternative UI for any functionality in
                // the title bar, such as search.
            }
            Title = Windows.ApplicationModel.Package.Current.DisplayName;
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
            IReadOnlyList<Windows.System.User> users = await Windows.System.User.FindAllAsync();

            Windows.System.User current = users.Where(p => p.AuthenticationStatus == UserAuthenticationStatus.LocallyAuthenticated &&
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

        #region Custom Window Methods
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
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
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
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
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

        private async Task GetUpdates()
        {
            GitHubClient GitClient = new(new ProductHeaderValue("WCS"));
            string Revision = Windows.ApplicationModel.Package.Current.Id.Version.Revision.ToString();
            string Current = Windows.ApplicationModel.Package.Current.Id.Version.Major.ToString() + "."
                        + Windows.ApplicationModel.Package.Current.Id.Version.Minor.ToString() + "."
                        + Windows.ApplicationModel.Package.Current.Id.Version.Build.ToString() + "."
                        + Revision + "-canary";
            try
            {
                IReadOnlyList<Release> Release = await GitClient.Repository.Release.GetAll("rencerace", "WCS");
                Release Latest = Release[0];
                if (Latest.TagName.EndsWith("-canary") && Latest.TagName != Current)
                {
                    //if (Current != Latest.TagName)
                    //{
                    //    ContentDialog userDialog = new()
                    //    {
                    //        Title = "Wait, What?",
                    //        Content = "You have the INTERNAL version of the application.\n" +
                    //                  "IF YOU KNOW WHAT YOU ARE DOING, PLEASE IGNORE THIS MESSAGE.",
                    //        CloseButtonText = "OK",
                    //        DefaultButton = ContentDialogButton.Close
                    //    };
                    //    userDialog.XamlRoot = Content.XamlRoot;
                    //    await userDialog.ShowAsync();
                    //}
                    //else 
                    //{ 
                    ContentDialog updateDialog = new()
                    {
                        Title = "Windows Code Studio Canary Update is Available!",
                        Content = "Would you like to Update now?",
                        PrimaryButtonText = "Yes, Update Now",
                        SecondaryButtonText = "No, Maybe Later",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = Content.XamlRoot
                    };
                    ContentDialogResult dialogResult = await updateDialog.ShowAsync();
                    if (dialogResult == ContentDialogResult.Primary)
                    {
                        IReadOnlyList<ReleaseAsset> Assets = Latest.Assets;
                        ReleaseAsset msix = null;
                        ReleaseAsset pfx = null;
                        foreach (ReleaseAsset Asset in Assets)
                        {
                            if (Asset.Name.EndsWith(".msix"))
                            {
                                msix = Asset;
                                break;
                            }
                            else if (Asset.Name.EndsWith(".pfx"))
                            {
                                pfx = Asset;
                                if (pfx != null)
                                {
                                    using HttpClient httpClient = new();
                                    string url = pfx.BrowserDownloadUrl;
                                    HttpResponseMessage response = httpClient.GetAsync(url).Result;
                                    Stream content = response.Content.ReadAsStreamAsync().Result;
                                    string TempPath = Path.GetTempPath();
                                    string fileName = "WCSCertificate.cer";
                                    string filePath = Path.Combine(TempPath, fileName);

                                    using (FileStream fileStream = new(filePath, System.IO.FileMode.CreateNew))
                                    {
                                        content.CopyTo(fileStream);
                                    }

                                    string certFilePath = filePath;
                                    X509Certificate2 cert = new(filePath);
                                    X509Store store = new(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
                                    store.Open(OpenFlags.ReadWrite);
                                    store.Add(cert);
                                }
                                else
                                {
                                    ContentDialog errorDialog = new()
                                    {
                                        Title = "Certificate Not Found",
                                        Content = "A Security Certificate for this application is not available in\n" +
                                                Path.GetTempPath(),
                                        CloseButtonText = "OK",
                                        DefaultButton = ContentDialogButton.Close,
                                        XamlRoot = Content.XamlRoot
                                    };
                                    _ = await errorDialog.ShowAsync();
                                }
                                break;
                            }
                        }
                        if (msix != null)
                        {
                            using HttpClient httpClient = new();
                            string url = msix.BrowserDownloadUrl;
                            HttpResponseMessage response = httpClient.GetAsync(url).Result;
                            Stream content = response.Content.ReadAsStreamAsync().Result;
                            string TempPath = Path.GetTempPath();
                            string fileName = "WCSUpdate.msix";
                            string filePath = Path.Combine(TempPath, fileName);

                            bool IsFileAlreadyAvailable = File.Exists(filePath);

                            if (IsFileAlreadyAvailable)
                            {
                                File.Delete(filePath);
                            }

                            using (FileStream fileStream = new(filePath, System.IO.FileMode.CreateNew, FileAccess.ReadWrite))
                            {
                                content.CopyTo(fileStream);
                            }

                            ContentDialog userDialog = new()
                            {
                                Title = "App Force Shutdown",
                                Content = "NOTE: This application is configured to forcibly shutdown itself when installing a newer version.\nWould you like to proceed?",
                                PrimaryButtonText = "Yes, Install Now",
                                CloseButtonText = "Cancel",
                                DefaultButton = ContentDialogButton.Primary,
                                XamlRoot = Content.XamlRoot
                            };
                            ContentDialogResult userDialogResult = await userDialog.ShowAsync();
                            if (userDialogResult == ContentDialogResult.Primary)
                            {
                                PackageManager packman = new();
                                DeploymentResult depRes = await packman.AddPackageAsync(new Uri(filePath), null, DeploymentOptions.ForceApplicationShutdown);
                                DeploymentResult curPack = await packman.RemovePackageAsync(Windows.ApplicationModel.Package.Current.Id.FullName);
                            }
                        }
                    }
                    //}
                }
                else
                {
                    ContentDialog confirmDialog = new()
                    {
                        Title = "Windows Code Studio Canary is Up-to-Date",
                        Content = "You are using the latest version of Windows Code Studio Canary",
                        CloseButtonText = "OK",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = Content.XamlRoot
                    };
                    _ = await confirmDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                ContentDialog exceptionDialog = new()
                {
                    Title = "Error Encountered",
                    Content = "Source: " + ex.Source + "\nMessage: " + ex.Message,
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = Content.XamlRoot
                };
                _ = await exceptionDialog.ShowAsync();
            }
        }

        private async void UpdateItem_Click(object sender, RoutedEventArgs e)
        {
            await GetUpdates();
        }

        private async void AboutItem_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog aboutDialog = new()
            {
                Title = "About",
                Content = "Product Name: " + Windows.ApplicationModel.Package.Current.DisplayName
                        + "\nVersion: " + Windows.ApplicationModel.Package.Current.Id.Version.Major
                        + "." + Windows.ApplicationModel.Package.Current.Id.Version.Minor
                        + "." + Windows.ApplicationModel.Package.Current.Id.Version.Build
                        + "." + Windows.ApplicationModel.Package.Current.Id.Version.Revision
                        + "\nPublisher: " + Windows.ApplicationModel.Package.Current.PublisherDisplayName
                        + "\nLicense: Mozilla Public License Version 2.0",
                PrimaryButtonText = "See License Information",
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot
            };
            ContentDialogResult dialogResult = await aboutDialog.ShowAsync();
            if (dialogResult == ContentDialogResult.Primary)
            {
                string url = "https://raw.githubusercontent.com/rencerace/WCS/canary/LICENSE";
                string browser = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
                _ = Process.Start(new ProcessStartInfo(browser, url));
            }
        }
    }
}
