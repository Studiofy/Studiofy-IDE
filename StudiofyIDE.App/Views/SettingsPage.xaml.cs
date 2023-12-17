using Microsoft.UI.Xaml.Controls;
using WindowsCode.Studio.Views.SettingsViews;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WindowsCode.Studio.Views.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            SettingsNavigation.SelectedItem = SettingsNavigation.Items[0];
        }

        private void SettingsNavigation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView settingsNavigation = sender as ListView;
            if ((settingsNavigation.SelectedItem as ListViewItem) == GeneralSettingsItem)
            {
                SectionFrame.Navigate(typeof(GeneralSettingsPage));
            }
            else if ((settingsNavigation.SelectedItem as ListViewItem) == EditorSettingsItem)
            {
                SectionFrame.Navigate(typeof(EditorSettingsPage));
            }
            else if ((settingsNavigation.SelectedItem as ListViewItem) == TerminalSettingsItem)
            {
                SectionFrame.Navigate(typeof(TerminalSettingsPage));
            }
            else if ((settingsNavigation.SelectedItem as ListViewItem) == CommandBarSettingsItem)
            {
                SectionFrame.Navigate(typeof(CommandBarSettingsPage));
            }
        }
    }
}
