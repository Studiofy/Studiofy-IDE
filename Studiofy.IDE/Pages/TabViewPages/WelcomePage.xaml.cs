// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Studiofy.IDE.Pages.TabViewPages
{
    public sealed partial class WelcomePage : Page
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        public static List<object> recentFiles { get; set; } = new();

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RecentFilesList.ItemsSource = recentFiles;

            ItemNotifier.Visibility = RecentFilesList.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            RecentFilesList.InvalidateArrange();
        }

        private void NewFileButton_Click(object sender, RoutedEventArgs e)
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

            MainWindow.m_TabService.Add(editorTab);

            if (editorTab.Header != null)
            {
                recentFiles.Add(editorTab.Header);
            }
        }

        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker = new();

            nint hwnd = WindowNative.GetWindowHandle(MainWindow.m_MainWindow);

            InitializeWithWindow.Initialize(filePicker, hwnd);

            filePicker.FileTypeFilter.Add("*");

            StorageFile storageFile = await filePicker.PickSingleFileAsync();

            if (storageFile != null)
            {
                TabViewItem tabItem = await MainWindow.m_FileService.OpenFileAsync(storageFile, new EditorPage());

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
                    MainWindow.m_TabService.Add(tabItem);

                    if (tabItem.Header != null)
                    {
                        recentFiles.Add(tabItem.Header);
                    }
                }
            }
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new();
            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");

            nint hwnd = WindowNative.GetWindowHandle(MainWindow.m_MainWindow);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder selectedFolder = await folderPicker.PickSingleFolderAsync();

            await MainWindow.m_FileService.PopulateFileView(selectedFolder, null, Content.XamlRoot);
        }

        private void CloneRepositoryButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void RecentFilesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            ContentDialog itemClickedDialog = new()
            {
                Title = $"{e.ClickedItem} Clicked",
                Content = "This is a placeholder content when the item is clicked.",
                XamlRoot = Content.XamlRoot,
                DefaultButton = ContentDialogButton.Close,
                CloseButtonText = "Close"
            };

            await itemClickedDialog.ShowAsync();
        }
    }
}
