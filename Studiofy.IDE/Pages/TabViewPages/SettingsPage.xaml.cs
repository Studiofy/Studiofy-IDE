using Microsoft.UI.Xaml.Controls;
using Studiofy.IDE.Pages.SettingsPages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Studiofy.IDE.Pages.TabViewPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();

            SectionListView.SelectedIndex = 0;

            if (SectionListView.SelectedIndex == 0)
            {
                SectionContent.Navigate(typeof(GeneralSettingsPage));
            }
            else if (SectionListView.SelectedIndex == 1)
            {
                SectionContent.Navigate(typeof(EditorSettingsPage));
            }
            else if (SectionListView.SelectedIndex == 2)
            {
                SectionContent.Navigate(typeof(TerminalSettingsPage));
            }
            else if (SectionListView.SelectedIndex == 3)
            {
                SectionContent.Navigate(typeof(CommandBarSettingsPage));
            }
        }

        private void SectionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SectionListView.SelectedIndex == 0)
            {
                SectionContent.Navigate(typeof(GeneralSettingsPage));
            }
            else if (SectionListView.SelectedIndex == 1)
            {
                SectionContent.Navigate(typeof(EditorSettingsPage));
            }
            else if (SectionListView.SelectedIndex == 2)
            {
                SectionContent.Navigate(typeof(TerminalSettingsPage));
            }
            else if (SectionListView.SelectedIndex == 3)
            {
                SectionContent.Navigate(typeof(CommandBarSettingsPage));
            }
        }
    }
}
