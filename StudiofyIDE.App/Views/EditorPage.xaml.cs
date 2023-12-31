// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using WindowsCode.Studio.Models;
using WindowsCode.Studio.Services;
using WindowsCode.Studio.Views.TabViews;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WindowsCode.Studio.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditorPage : Page
    {
        private string[] fileExtensions = { ".txt", ".htm", ".html", ".php", ".js", ".css", ".razor" };

        private string _activeFolder;

        private TabService TabService;

        private FileService FileService;

        public static EditorPage _activePage;

        private EditBoxTabView editor;

        private StorageFile _activeFile;

        private SettingsModel settingsModel;

        public List<IStorageItem> storageItems;

        public EditorPage()
        {
            InitializeComponent();

            _activePage = this;
            App.SetEditorPage(this);
            EditorNavigationView.SelectedItem = EditorNavigationView.MenuItems[0];

            FileService = new();
            TabService = new(FileTabView);
        }

        private async void NewFileButton_Click(object sender, RoutedEventArgs e)
        {
            Grid parent = new()
            {
                Width = 325
            };

            TextBox FileNameTextBox = new()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                PlaceholderText = "Enter File Name",
                Margin = new Thickness(0, 0, 5, 0),
                Width = 210
            };

            ComboBox FileExtensionBox = new()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                PlaceholderText = "Choose",
                Width = 110
            };

            foreach (string fileExtension in fileExtensions)
            {
                FileExtensionBox.Items.Add(fileExtension);
            }

            parent.Children.Add(FileNameTextBox);
            parent.Children.Add(FileExtensionBox);

            ContentDialog NewFileDialog = new()
            {
                Title = "Create New File",
                Content = parent,
                PrimaryButtonText = "Create File",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            NewFileDialog.XamlRoot = Content.XamlRoot;
            ContentDialogResult result = await NewFileDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (string.IsNullOrWhiteSpace(FileNameTextBox.Text) || string.IsNullOrWhiteSpace(FileExtensionBox.SelectedValue.ToString()))
                {
                    ContentDialog errorDialog = new()
                    {
                        Title = "Error",
                        Content = "Invalid file name or file extension.",
                        CloseButtonText = "OK",
                        DefaultButton = ContentDialogButton.Close
                    };
                    errorDialog.XamlRoot = Content.XamlRoot;
                    await errorDialog.ShowAsync();
                    NewFileButton_Click(sender, e);
                }
                else
                {
                    TabService.CreateTabItem($"{FileNameTextBox.Text}{FileExtensionBox.SelectedValue}", new EditBoxTabView());
                    FileTabView.SelectedIndex = 0;
                }
            }
        }

        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker FilePicker = new();

            nint hwnd = WindowNative.GetWindowHandle(App.GetMainWindow());
            InitializeWithWindow.Initialize(FilePicker, hwnd);

            foreach (string item in fileExtensions)
            {
                FilePicker.FileTypeFilter.Add(item);
            }

            editor = new EditBoxTabView();

            FileService.SetEditor(editor);
            FileService.SetTabService(TabService);

            await FileService.OpenSelectedFile(await FilePicker.PickSingleFileAsync(), Content, editor.CodeEditor, FileTabView);

            //if (File == null)
            //{
            //    ContentDialog errorDialog = new()
            //    {
            //        Title = "Error",
            //        Content = "File is Null",
            //        CloseButtonText = "OK",
            //        DefaultButton = ContentDialogButton.Close
            //    };
            //    errorDialog.XamlRoot = Content.XamlRoot;
            //    await errorDialog.ShowAsync();
            //}
            //else
            //{
            //    editor = new();

            //    RichEditBox codeEditor = editor.CodeEditor;

            //    if (codeEditor != null)
            //    {
            //        string fileContent = await FileIO.ReadTextAsync(File);

            //        if (string.IsNullOrEmpty(fileContent))
            //        {
            //            ContentDialog errorDialog = new()
            //            {
            //                Title = "Error",
            //                Content = "Cannot Add File Contents",
            //                CloseButtonText = "OK",
            //                DefaultButton = ContentDialogButton.Close
            //            };
            //            errorDialog.XamlRoot = Content.XamlRoot;
            //            await errorDialog.ShowAsync();
            //        }
            //        else
            //        {
            //            codeEditor.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, fileContent);
            //        }
            //    }
            //    else
            //    {
            //        ContentDialog errorDialog = new()
            //        {
            //            Title = "Error",
            //            Content = "Cannot Read File",
            //            CloseButtonText = "OK",
            //            DefaultButton = ContentDialogButton.Close
            //        };
            //        errorDialog.XamlRoot = Content.XamlRoot;
            //        await errorDialog.ShowAsync();
            //    }

            //    TabService.CreateTabItem(File.Path, editor);
            //    FileTabView.SelectedIndex += 1;
            //}
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker FolderPicker = new()
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };

            nint hwnd = WindowNative.GetWindowHandle(App.GetMainWindow());
            InitializeWithWindow.Initialize(FolderPicker, hwnd);

            FolderPicker.FileTypeFilter.Add("*");

            StorageFolder selectedFolder = await FolderPicker.PickSingleFolderAsync();

            if (selectedFolder != null)
            {
                await PopulateTreeView(selectedFolder, FileTreeView.RootNodes);
                _activeFolder = selectedFolder.Path;
            }
        }

        private async Task PopulateTreeView(StorageFolder folder, IList<TreeViewNode> nodes)
        {
            // Clear existing nodes in the TreeView
            nodes.Clear();

            List<IStorageItem> items = (await folder.GetItemsAsync()).ToList();

            foreach (IStorageItem item in items)
            {
                TreeViewNode newNode = new() { Content = item.Name };

                if (item is StorageFolder)
                {
                    newNode.HasUnrealizedChildren = true;
                }

                nodes.Add(newNode);

                // If the item is a folder, recursively populate its children
                if (item is StorageFolder subFolder)
                {
                    await PopulateTreeView(subFolder, newNode.Children);
                }
            }
        }

        private StorageFile GetActiveTabFile()
        {
            TabViewItem currentTab = FileTabView.SelectedItem as TabViewItem;

            if (currentTab != null)
            {
                // Assuming the Header property contains the absolute path of the file
                string filePath = currentTab.Header as string;

                if (!string.IsNullOrEmpty(filePath))
                {
                    try
                    {
                        // Convert the file path to StorageFile
                        _activeFile = StorageFile.GetFileFromPathAsync(filePath).AsTask().GetAwaiter().GetResult();

                        // Return the file
                        return _activeFile ?? null;
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception (e.g., invalid file path)
                        // Log or show an error message as needed
                        Debug.WriteLine($"Error getting StorageFile: {ex.Message}");
                    }
                }
            }

            // Set _activeFile to null if there's an issue
            _activeFile = null;
            return _activeFile ?? null;
        }

        private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            StorageFile selectedFile = GetActiveTabFile();

            if (selectedFile != null)
            {
                CachedFileManager.DeferUpdates(selectedFile);

                // Get the current tab
                TabViewItem currentTab = FileTabView.SelectedItem as TabViewItem;

                if (currentTab != null)
                {
                    // Assuming CodeEditor is the RichEditBox in your EditBoxTabView
                    RichEditBox codeEditor = (currentTab.Content as EditBoxTabView)?.CodeEditor;

                    if (codeEditor != null)
                    {
                        // Get the text from RichEditBox
                        string newText = string.Empty;
                        codeEditor.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out newText);

                        // Write the content to the file
                        await FileIO.WriteTextAsync(selectedFile, newText);

                        // Complete the updates
                        FileUpdateStatus updateStatus = await CachedFileManager.CompleteUpdatesAsync(selectedFile);

                        if (updateStatus != FileUpdateStatus.Complete)
                        {
                            // Handle error or cancellation
                            ContentDialog errorDialog = new()
                            {
                                Title = "Error",
                                Content = "Cannot save file contents",
                                CloseButtonText = "OK",
                                DefaultButton = ContentDialogButton.Close
                            };
                            errorDialog.XamlRoot = Content.XamlRoot;
                            await errorDialog.ShowAsync();
                        }
                    }
                    else
                    {
                        // Handle error or cancellation
                        ContentDialog errorDialog = new()
                        {
                            Title = "Error",
                            Content = "Cannot save file contents",
                            CloseButtonText = "OK",
                            DefaultButton = ContentDialogButton.Close
                        };
                        errorDialog.XamlRoot = Content.XamlRoot;
                        await errorDialog.ShowAsync();
                    }
                }
            }
            else
            {
                FileSavePicker filePicker = new();

                // Update the tabHeader of selected tabItem
                TabViewItem selectedTabItem = FileTabView.SelectedItem as TabViewItem;

                nint hwnd = WindowNative.GetWindowHandle(App.GetMainWindow());
                InitializeWithWindow.Initialize(filePicker, hwnd);

                filePicker.FileTypeChoices.Add("Text File (.txt)", new List<string>() { ".txt" });
                filePicker.FileTypeChoices.Add("HTML File (.htm .html)", new List<string>() { ".htm", ".html" });
                filePicker.FileTypeChoices.Add("PHP File", new List<string>() { ".php" });
                filePicker.FileTypeChoices.Add("JavaScript File", new List<string>() { ".js" });
                filePicker.FileTypeChoices.Add("CSS File", new List<string>() { ".css" });
                filePicker.FileTypeChoices.Add("Razor File", new List<string>() { ".razor" });
                filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                string tabHeader = selectedTabItem.Header.ToString();

                // Check if the selectedTabItem.Header.ToString() contains .html, .txt 
                if (!string.IsNullOrEmpty(tabHeader))
                {
                    if (tabHeader.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    {
                        // Trim the selectedTabItem.Header.ToString() and add to SuggestedFileName
                        filePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(tabHeader);
                        // Select the .html in the FileTypeChoices
                        filePicker.FileTypeChoices.Clear();
                        filePicker.FileTypeChoices.Add("HTML File (.html)", new List<string>() { ".html" });
                    }
                    else if (tabHeader.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
                    {
                        // Trim the selectedTabItem.Header.ToString() and add to SuggestedFileName
                        filePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(tabHeader);
                        // Select the .html in the FileTypeChoices
                        filePicker.FileTypeChoices.Clear();
                        filePicker.FileTypeChoices.Add("HTML File (.htm)", new List<string>() { ".htm" });
                    }
                    else if (tabHeader.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        // Trim the selectedTabItem.Header.ToString() and add to SuggestedFileName
                        filePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(tabHeader);
                        // Select the .txt in the FileTypeChoices
                        filePicker.FileTypeChoices.Clear();
                        filePicker.FileTypeChoices.Add("Text File (.txt)", new List<string>() { ".txt" });
                    }
                    else if (tabHeader.EndsWith(".php", StringComparison.OrdinalIgnoreCase))
                    {
                        // Trim the selectedTabItem.Header.ToString() and add to SuggestedFileName
                        filePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(tabHeader);
                        // Select the .txt in the FileTypeChoices
                        filePicker.FileTypeChoices.Clear();
                        filePicker.FileTypeChoices.Add("PHP File (.php)", new List<string>() { ".php" });
                    }
                    else if (tabHeader.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                    {
                        // Trim the selectedTabItem.Header.ToString() and add to SuggestedFileName
                        filePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(tabHeader);
                        // Select the .txt in the FileTypeChoices
                        filePicker.FileTypeChoices.Clear();
                        filePicker.FileTypeChoices.Add("JavaScript File (.js)", new List<string>() { ".js" });
                    }
                    else if (tabHeader.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                    {
                        // Trim the selectedTabItem.Header.ToString() and add to SuggestedFileName
                        filePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(tabHeader);
                        // Select the .txt in the FileTypeChoices
                        filePicker.FileTypeChoices.Clear();
                        filePicker.FileTypeChoices.Add("CSS File (.css)", new List<string>() { ".css" });
                    }
                    else if (tabHeader.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                    {
                        // Trim the selectedTabItem.Header.ToString() and add to SuggestedFileName
                        filePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(tabHeader);
                        // Select the .txt in the FileTypeChoices
                        filePicker.FileTypeChoices.Clear();
                        filePicker.FileTypeChoices.Add("Razor File (.razor)", new List<string>() { ".razor" });
                    }
                }

                StorageFile file = await filePicker.PickSaveFileAsync();

                if (selectedTabItem != null)
                {
                    if (file != null)
                    {
                        selectedTabItem.Header = file.Path;
                        SaveFileButton_Click(sender, e);
                    }
                }
            }

            TabViewItem selectedTab = FileTabView.SelectedItem as TabViewItem;
            if (selectedTab != null)
            {
                if (selectedTab.Header.ToString() != "Welcome")
                {
                    FilePreviewer.Source = new Uri(selectedTab.Header.ToString());
                }
            }
        }

        private async void FileTreeView_SelectionChanged(object sender, TreeViewSelectionChangedEventArgs args)
        {
            if (args.AddedItems.Count > 0)
            {
                TreeViewNode selectedNode = args.AddedItems[0] as TreeViewNode;

                if (selectedNode != null)
                {
                    // Get the selectedNode as file
                    StorageFile File = null;

                    // If the selected node is inside a parent/s add the parent/s to the path
                    if (selectedNode.Parent != null)
                    {
                        // Construct the full path including parent nodes
                        string filePath = await GetFullPathFromNodeAsync(selectedNode, _activeFolder);

                        // Get the StorageFile
                        try
                        {
                            if (selectedNode.HasChildren)
                            {
                                selectedNode.IsExpanded = true;
                            }
                            else
                            {
                                File = await StorageFile.GetFileFromPathAsync(_activeFolder + $"\\{filePath}");

                                if (File != null)
                                {
                                    TabViewItem existingTab = FindTabByHeader(FileTabView, File.Path);

                                    if (existingTab != null)
                                    {
                                        FileTabView.SelectedItem = existingTab;
                                    }
                                    else
                                    {
                                        editor = new();

                                        RichEditBox codeEditor = editor.CodeEditor;

                                        try
                                        {
                                            string fileContent = await FileIO.ReadTextAsync(File);

                                            if (string.IsNullOrEmpty(fileContent))
                                            {
                                                ContentDialog errorDialog = new()
                                                {
                                                    Title = "Error",
                                                    Content = "Cannot Add File Contents",
                                                    CloseButtonText = "OK",
                                                    DefaultButton = ContentDialogButton.Close
                                                };
                                                errorDialog.XamlRoot = Content.XamlRoot;
                                                await errorDialog.ShowAsync();
                                            }
                                            else
                                            {
                                                codeEditor.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, fileContent);

                                                // Find if there's a tab Item that has the header of File.Path

                                                TabService.CreateTabItem(File.Path, editor);
                                                FileTabView.SelectedIndex += 1;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            ContentDialog errorDialog = new()
                                            {
                                                Title = $"Error",
                                                Content = $"Cannot open {File.Name}\nIt may be corrupted or the file type is not supported.",
                                                CloseButtonText = "OK",
                                                DefaultButton = ContentDialogButton.Close
                                            };
                                            errorDialog.XamlRoot = Content.XamlRoot;
                                            await errorDialog.ShowAsync();
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ContentDialog errorDialog = new()
                            {
                                Title = $"{ex.Source}",
                                Content = ex.Message.Split('\n')[0],
                                CloseButtonText = "OK",
                                DefaultButton = ContentDialogButton.Close
                            };
                            errorDialog.XamlRoot = Content.XamlRoot;
                            await errorDialog.ShowAsync();
                        }
                    }
                    else
                    {
                        // Construct the full path without parent nodes
                        string filePath = Path.Combine(_activeFolder, selectedNode.Content.ToString());

                        // Get the StorageFile
                        File = await StorageFile.GetFileFromPathAsync(filePath);

                        if (File != null)
                        {
                            TabViewItem existingTab = FindTabByHeader(FileTabView, File.Path);

                            if (existingTab != null)
                            {
                                FileTabView.SelectedItem = existingTab;
                            }
                            else
                            {
                                editor = new();

                                RichEditBox codeEditor = editor.CodeEditor;

                                try
                                {
                                    string fileContent = await FileIO.ReadTextAsync(File);

                                    if (string.IsNullOrEmpty(fileContent))
                                    {
                                        ContentDialog errorDialog = new()
                                        {
                                            Title = "Error",
                                            Content = "Cannot Add File Contents",
                                            CloseButtonText = "OK",
                                            DefaultButton = ContentDialogButton.Close
                                        };
                                        errorDialog.XamlRoot = Content.XamlRoot;
                                        await errorDialog.ShowAsync();
                                    }
                                    else
                                    {
                                        codeEditor.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, fileContent);

                                        // Find if there's a tab Item that has the header of File.Path

                                        TabService.CreateTabItem(File.Path, editor);
                                        FileTabView.SelectedIndex += 1;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ContentDialog errorDialog = new()
                                    {
                                        Title = $"{ex.Source} throws an Exception",
                                        Content = ex.Message.Split('\n')[0],
                                        CloseButtonText = "OK",
                                        DefaultButton = ContentDialogButton.Close
                                    };
                                    errorDialog.XamlRoot = Content.XamlRoot;
                                    await errorDialog.ShowAsync();
                                }
                            }
                        }
                    }
                }
            }
        }

        private async Task<string> GetFullPathFromNodeAsync(TreeViewNode node, string basePath)
        {
            if (node.Content != null)
            {
                // Recursively construct the full path including parent nodes
                if (node.Parent != null)
                {
                    string parentPath = await GetFullPathFromNodeAsync(node.Parent, basePath);
                    return Path.Combine(parentPath, node.Content.ToString());
                }
                else
                {
                    return Path.Combine(basePath, node.Content.ToString());
                }
            }
            else
            {
                // Handle the case where Content is null (log, throw exception, etc.)
                // For now, returning an empty string, but you may want to customize this behavior
                return string.Empty;
            }
        }

        private TabViewItem FindTabByHeader(TabView tabView, string header)
        {
            // Iterate through tabView's TabItems to find the one with the specified header
            foreach (TabViewItem tabItem in tabView.TabItems)
            {
                if (tabItem.Header != null && tabItem.Header.ToString() == header)
                {
                    return tabItem ?? null;
                }
            }

            // If not found, return null
            return null;
        }

        private void FileTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Item);

            if (args.Tab.Header.ToString() != "Welcome")
            {
                if (sender.TabItems.Count is 0)
                {
                    TabService tabService = new(sender);
                    tabService.CreateTabItem("Welcome", new WelcomePage());
                    tabService.GetTabView().SelectedIndex += 1;
                }
                else
                {
                    sender.SelectedIndex = 0;
                }
            }
        }

        private void FileTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileTabView.TabItems.Count != 0)
            {
                TabViewItem selectedTab = FileTabView.SelectedItem as TabViewItem;
                if (selectedTab != null && selectedTab.Header.ToString() != "Welcome" && selectedTab.Header.ToString() != "Settings")
                {
                    try
                    {
                        FilePreviewer.Source = new Uri(selectedTab.Header.ToString());
                    }
                    catch (Exception)
                    {
                        FilePreviewer.Source = new Uri("about:blank");
                        PreviewFileButton.IsEnabled = false;
                    }
                    SaveFileButton.IsEnabled = true;
                    RenameFileButton.IsEnabled = true;
                    DeleteFileButton.IsEnabled = true;
                    PreviewFileButton.IsEnabled = true;
                }
                else
                {
                    FilePreviewer.Source = new Uri("about:blank");
                    SaveFileButton.IsEnabled = false;
                    RenameFileButton.IsEnabled = false;
                    DeleteFileButton.IsEnabled = false;
                    PreviewFileButton.IsEnabled = false;
                }
            }
            else
            {
                SaveFileButton.IsEnabled = false;
                RenameFileButton.IsEnabled = false;
                DeleteFileButton.IsEnabled = false;
                PreviewFileButton.IsEnabled = false;
            }
        }

        private void PreviewFileButton_Click(object sender, RoutedEventArgs e)
        {
            bool toggleResult = PreviewFileButton.IsChecked != true ? false : true;

            TabViewItem selectedTab = FileTabView?.SelectedItem as TabViewItem;

            if (toggleResult)
            {
                if (selectedTab != null)
                {
                    if (selectedTab.Header.ToString() != "Welcome" && selectedTab.Header.ToString() != "Settings")
                    {
                        try
                        {
                            FilePreviewer.Source = new Uri(selectedTab.Header.ToString());
                        }
                        catch (Exception)
                        {
                            FilePreviewer.Source = new Uri("about:blank");
                        }
                        PreviewSplitView.IsPaneOpen = true;
                        PreviewFileButton.IsChecked = true;
                    }
                }
            }
            else
            {
                if (selectedTab != null)
                {
                    PreviewSplitView.IsPaneOpen = false;
                    PreviewFileButton.IsChecked = false;
                }
            }
        }

        private void RunProjectButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleSplitPaneButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EditorSplitView.IsPaneOpen)
            {
                EditorSplitView.IsPaneOpen = !EditorSplitView.IsPaneOpen;
            }
            else
            {
                EditorSplitView.IsPaneOpen = !EditorSplitView.IsPaneOpen;
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            settingsModel = await SettingsService.ReadSettingsFileAsync(Content);

            if (settingsModel != null && settingsModel.DisplayShowWelcomePageOnStartup)
            {
                TabViewItem welcomeTab = await TabService.CreateTabItemAsync("Welcome", new WelcomePage(), new SymbolIconSource()
                {
                    Symbol = Symbol.Document
                });
                TabService.GetTabHeader(welcomeTab);

                if (SystemInformation.Instance.IsAppUpdated)
                {
                    TabViewItem whatsNewTab = await TabService.CreateTabItemAsync("What's New", new WhatsNewPage(), new FontIconSource()
                    {
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                        Glyph = "&#xE789;"
                    });
                    TabService.GetTabHeader(whatsNewTab);
                    FileTabView.SelectedIndex = FileTabView.TabItems.IndexOf(whatsNewTab);
                }
                else
                {
                    FileTabView.SelectedIndex = FileTabView.TabItems.IndexOf(welcomeTab);
                }
            }
        }
    }
}
