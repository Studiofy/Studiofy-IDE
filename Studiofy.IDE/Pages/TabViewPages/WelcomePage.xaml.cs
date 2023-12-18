// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Studiofy.IDE.Pages.TabViewPages
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

        }

        private void NewFileButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CloneRepositoryButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
