using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Studiofy.IDE.Pages.SettingsPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GeneralSettingsPage : Page
    {
        public GeneralSettingsPage()
        {
            InitializeComponent();
        }

        private void AppearanceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ConfidentialMessageCheckbox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ShowWelcomePageCheckbox_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
