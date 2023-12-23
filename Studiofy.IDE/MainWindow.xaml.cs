using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Studiofy.Common.Service;
using Studiofy.IDE.Dialogs;
using Studiofy.IDE.Pages.TabViewPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
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
        private AppWindow m_AppWindow { get; set; }

        private UpdateService m_UpdateService { get; set; }

        public static TabService m_TabService = new();

        public static FileService m_FileService;

        public static MainWindow m_MainWindow { get; set; }

        #region Window Initialization

        public MainWindow()
        {
            InitializeComponent();

            m_MainWindow = this;

            SystemBackdrop = new MicaBackdrop()
            {
                Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base
            };

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

            //AppNavigationView.SelectedItem = ExplorerNavMenuItem;

            TreeViewSearchBox.PlaceholderText = "Search Files";

            m_TabService.Set(EditorTabView);
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
            Microsoft.UI.WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
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
            Microsoft.UI.WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
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

        private StorageFolder activeFolder { get; set; }

        private List<string> searchableTexts = new()
        {
            "New File",
            "Open Folder",
            "Open Settings",
            "Check Updates",
            "About"
        };

        private List<string> searchableFiles = new();

        private void AppNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            //if (sender.SelectedItem as NavigationViewItem == ExplorerNavMenuItem)
            //{
            //    TreeViewSearchBox.PlaceholderText = "Search Files";
            //}
            //else if (sender.SelectedItem as NavigationViewItem == ExtensionNavMenuItem)
            //{
            //    TreeViewSearchBox.PlaceholderText = "Search Extensions";
            //    NavTreeView.ItemsSource = null;
            //}
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TabViewItem settingsTab = new()
            {
                Header = "Settings",
                Content = new SettingsPage(),
                IconSource = new SymbolIconSource()
                {
                    Symbol = Symbol.Setting
                }
            };

            m_TabService.Add(settingsTab);
        }

        private void EditorTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            m_TabService.Remove(args.Item as TabViewItem);
        }

        private void ShowWelcomePageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TabViewItem welcomeTab = new()
            {
                Header = "Welcome",
                Content = new WelcomePage(),
                IconSource = new SymbolIconSource()
                {
                    Symbol = Symbol.Document
                }
            };

            if (EditorTabView.TabItems.Cast<TabViewItem>().Any(tabItem => tabItem.Header.ToString() == welcomeTab.Header.ToString()))
            {
                EditorTabView.SelectedItem = EditorTabView.TabItems.Cast<TabViewItem>().FirstOrDefault(tabItem => tabItem.Header.ToString() == welcomeTab.Header.ToString());
            }
            else
            {
                m_TabService.Add(welcomeTab);
            }
        }

        private void EditorTabView_Loaded(object sender, RoutedEventArgs e)
        {
            m_TabService.Add(new TabViewItem()
            {
                Header = "Welcome",
                Content = new WelcomePage(),
                IconSource = new SymbolIconSource()
                {
                    Symbol = Symbol.Document
                }
            });

            m_FileService = new();

            AppStatus.Text = "Ready";
        }

        private void EditorTabView_AddTabButtonClick(TabView sender, object args)
        {
            TabViewItem editorTab = new()
            {
                Header = "New File",
                Content = new EditorPage(),
                IconSource = new SymbolIconSource()
                {
                    Symbol = Symbol.Document
                }
            };

            m_TabService.Add(editorTab);
        }

        private void AppSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            AppSuggestBox.Text = args.SelectedItem.ToString();
            if (AppSuggestBox.Text.Equals(searchableTexts[0]))
            {
                TabViewItem editorTab = new()
                {
                    Header = "New File",
                    Content = new EditorPage(),
                    IconSource = new SymbolIconSource()
                    {
                        Symbol = Symbol.Document
                    }
                };

                m_TabService.Add(editorTab);
            }
        }

        private async void AppSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrEmpty(sender.Text))
            {
                bool fromSuggestion = sender.Text.Equals(args.ChosenSuggestion);
                ContentDialog resultDialog = new()
                {
                    Title = "Query Submitted!",
                    Content = $"Query Text: {sender.Text}\nFrom Suggestion: {fromSuggestion}",
                    CloseButtonText = "Close",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = Content.XamlRoot
                };
                await resultDialog.ShowAsync();
                if (sender.Text.Equals(searchableTexts[0]))
                {
                    TabViewItem editorTab = new()
                    {
                        Header = "New File",
                        Content = new EditorPage(),
                        IconSource = new SymbolIconSource()
                        {
                            Symbol = Symbol.Document
                        }
                    };

                    EditorTabView.TabItems.Add(editorTab);
                    EditorTabView.SelectedItem = editorTab;
                }
                sender.Text = string.Empty;
            }
        }

        private void AppSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                List<string> possibleSelections = new();
                string[] selectionText = sender.Text.ToLower().Split(" ");
                foreach (string text in searchableTexts)
                {
                    bool foundSearchableText = selectionText.All((key) =>
                    {
                        return text.ToLower().Contains(key);
                    });
                    if (foundSearchableText)
                    {
                        possibleSelections.Add(text);
                    }
                }
                if (possibleSelections.Count is 0)
                {
                    possibleSelections.Add("No Results Found");
                }
                sender.ItemsSource = possibleSelections;
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            InfoBar alertBar = new()
            {
                Title = "Studiofy Confidential",
                Message = "This version of Studiofy IDE is for internal testing only. Do not distribute the application without the owner's permission.",
                ActionButton = new HyperlinkButton()
                {
                    Content = "Learn More",
                    NavigateUri = new Uri("https://github.com/Studiofy/Studiofy-IDE?tab=readme-ov-file#permission")
                },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsOpen = true,
                IsClosable = false,
                Severity = InfoBarSeverity.Error
            };

            InfoBarStackPanel.Children.Add(alertBar);
        }

        private void CheckUpdatesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            m_UpdateService = new(Common.Enums.Version.Canary, Content.XamlRoot);
            m_UpdateService.CheckForUpdates(Windows.ApplicationModel.Package.Current.Id.Version);
        }

        private void NewFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            EditorPage editorPage = new();

            TabViewItem editorTab = new()
            {
                Header = "New File",
                Content = editorPage,
                IconSource = new SymbolIconSource()
                {
                    Symbol = Symbol.Document
                }
            };

            m_TabService.Add(editorTab);

            if (editorTab.Header != null)
            {
                WelcomePage.recentFiles.Add(editorTab.Header);
            }
        }

        private async void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutDialog aboutDialog = new() { XamlRoot = Content.XamlRoot };

            await aboutDialog.ShowAsync();
        }

        private async void OpenFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker = new();

            nint hwnd = WindowNative.GetWindowHandle(m_MainWindow);

            InitializeWithWindow.Initialize(filePicker, hwnd);

            filePicker.FileTypeFilter.Add("*");

            StorageFile storageFile = await filePicker.PickSingleFileAsync();

            if (storageFile != null)
            {
                TabViewItem tabItem = await m_FileService.OpenFileAsync(storageFile, new EditorPage());

                RichEditBox textEditor = (tabItem.Content as EditorPage).TextEditor;

                if (textEditor != null)
                {
                    string fileContent = await FileIO.ReadTextAsync(storageFile);

                    if (!string.IsNullOrEmpty(fileContent))
                    {
                        textEditor.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, fileContent);
                    }
                    else
                    {
                        ContentDialog errorDialog = new()
                        {
                            Title = "Error",
                            Content = "Cannot Add File Contents",
                            CloseButtonText = "OK",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = Content.XamlRoot
                        };
                        await errorDialog.ShowAsync();
                    }
                }

                if (tabItem != null)
                {
                    m_TabService.Add(tabItem);

                    EditorTabView.SelectedItem = tabItem;

                    if (tabItem.Header != null)
                    {
                        WelcomePage.recentFiles.Add(tabItem.Header);
                    }
                }
            }
        }

        private void EditorTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as TabView).SelectedItem as TabViewItem != null)
            {
                if (((sender as TabView).SelectedItem as TabViewItem).Header.Equals("Welcome"))
                {
                    SaveFileButton.IsEnabled = false;
                    SaveAllButton.IsEnabled = false;
                    UndoButton.IsEnabled = false;
                    RedoButton.IsEnabled = false;
                    MoreButton.IsEnabled = false;
                }
                else
                {
                    SaveFileButton.IsEnabled = false;
                    SaveAllButton.IsEnabled = false;
                    UndoButton.IsEnabled = false;
                    RedoButton.IsEnabled = false;
                    MoreButton.IsEnabled = false;
                }
            }
        }

        private void CloseWindowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private async void OpenFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new();
            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");

            nint hwnd = WindowNative.GetWindowHandle(m_MainWindow);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder selectedFolder = await folderPicker.PickSingleFolderAsync();

            if (selectedFolder != null)
            {
                activeFolder = selectedFolder;

                await m_FileService.PopulateFileView(selectedFolder, NavTreeView.RootNodes, Content.XamlRoot);

                try
                {
                    for (int i = 0; i < FileMenu.Items.Count; i++)
                    {
                        if (i == 3)
                        {
                            FileMenu.Items.Add(new MenuFlyoutSeparator());
                        }
                        else if (i == 4)
                        {
                            FileMenu.Items.Add(new MenuFlyoutItem()
                            {
                                Text = $"Close {activeFolder.DisplayName} Folder",
                                Icon = new FontIcon()
                                {
                                    Glyph = "&#xE8BB;"
                                }
                            });
                        }
                    }

                    searchableFiles = await m_FileService.PopulateTreeViewSearchBox(selectedFolder, Content.XamlRoot);

                    TreeViewSearchBox.ItemsSource = searchableFiles;
                }
                catch (Exception ex)
                {
                    InfoBar exceptionBar = new()
                    {
                        Message = ex.Message.Replace("\r\n", " "),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        IsOpen = true,
                        IsClosable = true,
                        Severity = InfoBarSeverity.Error
                    };

                    InfoBarStackPanel.Children.Add(exceptionBar);
                }

            }
        }

        private void TreeViewSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            try
            {
                if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    List<string> possibleSelections = new();
                    string[] selectionText = sender.Text.ToLower().Split(" ");
                    foreach (string file in searchableFiles)
                    {
                        string nodeName = file;

                        bool foundSearchableText = selectionText.All((key) =>
                        {
                            return nodeName.Contains(key, StringComparison.CurrentCultureIgnoreCase);
                        });

                        if (foundSearchableText)
                        {
                            possibleSelections.Add(nodeName);
                            NavTreeView.RootNodes.FirstOrDefault(node => node.Content.ToString().Equals(nodeName));
                        }
                    }
                    if (possibleSelections.Count is 0)
                    {
                        possibleSelections.Add("No Results Found");
                    }
                    sender.ItemsSource = possibleSelections;
                }
            }
            catch (Exception ex)
            {
                InfoBar exceptionBar = new()
                {
                    Message = ex.Message.Replace("\r\n", " "),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    IsOpen = true,
                    IsClosable = true,
                    Severity = InfoBarSeverity.Error
                };

                InfoBarStackPanel.Children.Add(exceptionBar);
            }
        }

        private async void TreeViewSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            try
            {
                StorageFile chosenFile = await activeFolder.GetFileAsync(args.SelectedItem.ToString());

                if (chosenFile != null)
                {
                    InfoBarStackPanel.Children.Add(new InfoBar()
                    {
                        Title = "File Found",
                        Message = chosenFile.Path,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        IsOpen = true,
                        IsClosable = true,
                        Severity = InfoBarSeverity.Success
                    });
                }
                else
                {
                    InfoBarStackPanel.Children.Add(new InfoBar()
                    {
                        Title = "File Not Found",
                        Message = chosenFile.Path,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        IsOpen = true,
                        IsClosable = true,
                        Severity = InfoBarSeverity.Error
                    });
                }

            }
            catch (Exception ex)
            {
                InfoBar exceptionBar = new()
                {
                    Message = ex.Message.Replace("\r\n", " "),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    IsOpen = true,
                    IsClosable = true,
                    Severity = InfoBarSeverity.Error
                };

                InfoBarStackPanel.Children.Add(exceptionBar);
            }

        }

        private void ToggleListPane_Checked(object sender, RoutedEventArgs e)
        {
            if (ToggleListPane.IsChecked == true)
            {
                MainSplitView.IsPaneOpen = true;
                ToggleListPane.Icon = new SymbolIcon()
                {
                    Symbol = Symbol.ClosePane
                };
            }
        }

        private void ToggleListPane_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ToggleListPane.IsChecked == false)
            {
                MainSplitView.IsPaneOpen = false;
                ToggleListPane.Icon = new SymbolIcon()
                {
                    Symbol = Symbol.OpenPane
                };
            }
        }

        private void ToggleListPane_Loaded(object sender, RoutedEventArgs e)
        {
            if (ToggleListPane.IsChecked == true)
            {
                MainSplitView.IsPaneOpen = true;
                ToggleListPane.Icon = new SymbolIcon()
                {
                    Symbol = Symbol.ClosePane
                };
            }
            else if (ToggleListPane.IsChecked == false)
            {
                MainSplitView.IsPaneOpen = false;
                ToggleListPane.Icon = new SymbolIcon()
                {
                    Symbol = Symbol.OpenPane
                };
            }
        }
    }
}
