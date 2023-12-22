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

        private List<string> recentFiles = new()
        {
            "Welcome",
            "Hello World.js",
            "Test File",
            "Hello There",
            "Welcome"
        };

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void OpenEditorButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NewFileButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.m_TabService.Add(new TabViewItem()
            {
                Header = "New File",
                Content = new EditorPage(),
                IconSource = new SymbolIconSource()
                {
                    Symbol = Symbol.Document
                }
            });
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

                if (tabItem != null)
                {
                    MainWindow.m_TabService.Add(tabItem);
                }
            }
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CloneRepositoryButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
