// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using WindowsCode.Studio.Models;
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
        private string filePath;
        private string[] fileExtensions = { ".txt", ".htm", ".html", ".php", ".js", ".css" };
        public static EditorPage _activePage;

        public EditorPage()
        {
            _activePage = this;
            InitializeComponent();
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
                FileTabViewModel TabItem = new();
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
                    TabItem.CreateTabItem(FileTabView, FileNameTextBox.Text + FileExtensionBox.SelectedValue.ToString());
                    FileTabView.SelectedIndex = 0;
                }
            }
        }

        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            //Editor CodeEditor = new Editor();
            FileOpenPicker FilePicker = new();

            nint hwnd = WindowNative.GetWindowHandle(MainWindow.activeWindow);
            InitializeWithWindow.Initialize(FilePicker, hwnd);

            FilePicker.FileTypeFilter.Add(".txt");
            FilePicker.FileTypeFilter.Add(".htm");
            FilePicker.FileTypeFilter.Add(".html");

            StorageFile File = await FilePicker.PickSingleFileAsync();

            if (File == null)
            {
                filePath = null;
            }
            else
            {
                filePath = File.Path;
            }
        }

        private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker FilePicker = new();

            nint hwnd = WindowNative.GetWindowHandle(MainWindow.activeWindow);
            InitializeWithWindow.Initialize(FilePicker, hwnd);

            FilePicker.FileTypeChoices.Add("Text File (.txt)", new List<string>() { ".txt" });
            FilePicker.FileTypeChoices.Add("HTML File (.htm .html)", new List<string>() { ".htm", ".html" });
            FilePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            StorageFile File = await FilePicker.PickSaveFileAsync();

            if (File != null)
            {
                CachedFileManager.DeferUpdates(File);
                //await FileIO.WriteTextAsync(File, Editor.Document);
                FileUpdateStatus UpdateStatus = await CachedFileManager.CompleteUpdatesAsync(File);
                if (UpdateStatus == FileUpdateStatus.Complete)
                {

                }
                else
                {

                }
            }
        }

        private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void FileTabView_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void FileTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {

        }

        private void FileTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileTabView.TabItems.Count != 0)
            {
                SaveFileButton.IsEnabled = true;
                RenameFileButton.IsEnabled = true;
                DeleteFileButton.IsEnabled = true;
            }
            else
            {
                SaveFileButton.IsEnabled = false;
                RenameFileButton.IsEnabled = false;
                DeleteFileButton.IsEnabled = false;
            }
        }
    }
}
