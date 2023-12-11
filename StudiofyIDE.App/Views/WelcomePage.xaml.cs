// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using WindowsCode.Studio.Views.Dialogs;

namespace WindowsCode.Studio.Views
{
    public sealed partial class WelcomePage : Page
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ItemNotifier.Visibility = RecentItemList.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenEditorButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow currentWindow = MainWindow.activeWindow;
            ContinuumNavigationTransitionInfo transitionInfo = new();
            _ = currentWindow.ContentProvider.Navigate(typeof(EditorPage), null, transitionInfo);
        }

        private async void NewFileButton_Click(object sender, RoutedEventArgs e)
        {
            New_File newFileDialog = new()
            {
                XamlRoot = Content.XamlRoot
            };

            ContentDialogResult dialogResult = await newFileDialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary && !string.IsNullOrEmpty(newFileDialog._fileName))
            {

            }
            else
            {
                ContentDialog errorDialog = new()
                {
                    Title = "Error",
                    Content = "Invalid File Name",
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close
                };
                errorDialog.XamlRoot = Content.XamlRoot;
                await errorDialog.ShowAsync();
            }

        }
    }
}
