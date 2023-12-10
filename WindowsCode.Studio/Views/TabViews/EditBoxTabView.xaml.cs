// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WindowsCode.Studio.Views.TabViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditBoxTabView : Page
    {
        public EditBoxTabView()
        {
            InitializeComponent();
        }

        private void EditBox_TextChanged(object sender, RoutedEventArgs e)
        {
            //var parent = EditorPage._activePage;
            //var tabItem = parent.FileTabView.ContainerFromItem(EditBox.DataContext) as TabViewItem;
            //if (tabItem != null)
            //{
            //    tabItem.Header = $"* {tabItem.Header}";
            //}
        }

        private void EditBox_TextChanging(RichEditBox sender, RichEditBoxTextChangingEventArgs args)
        {
            //var parent = EditorPage._activePage;
            //var tabItem = parent.FileTabView.ContainerFromItem(EditBox.DataContext) as TabViewItem;
            //if (tabItem != null)
            //{
            //    tabItem.Header = $"* {tabItem.Header}";
            //}
        }
    }
}
