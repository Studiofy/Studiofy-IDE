// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using WinUIEx;

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
            WindowEx dialog = new();
            await dialog.ShowMessageDialogAsync("Welcome", "New File");
        }
    }
}
