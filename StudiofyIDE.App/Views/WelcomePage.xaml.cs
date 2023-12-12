// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WindowsCode.Studio.Services;
using WindowsCode.Studio.Views.Dialogs;
using WindowsCode.Studio.Views.TabViews;

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
            if (App.GetEditorPage() != null)
            {
                EditorPage editorPage = App.GetEditorPage();
                App.GetMainWindow().ContentProvider.Navigate(editorPage.GetType());
            }
            else
            {
                App.SetEditorPage(new EditorPage());
                OpenEditorButton_Click(sender, e);
            }
        }

        private async void NewFileButton_Click(object sender, RoutedEventArgs e)
        {
            New_File newFileDialog = new() { XamlRoot = Content.XamlRoot };

            ContentDialogResult dialogResult = await newFileDialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary && !string.IsNullOrEmpty(newFileDialog.GetFileName()))
            {
                if (App.GetEditorPage() != null)
                {
                    TabService tabService = new(App.GetEditorPage().FileTabView);

                    tabService.CreateTabItem($"{newFileDialog.GetFileName()}", new EditBoxTabView());
                    tabService.GetTabView().SelectedIndex += 1;
                }
            }
        }
    }
}
