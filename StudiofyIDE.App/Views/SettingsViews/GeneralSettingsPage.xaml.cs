using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using WindowsCode.Studio.Models;
using WindowsCode.Studio.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WindowsCode.Studio.Views.SettingsViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GeneralSettingsPage : Page
    {
        private SettingsModel settingsModel;

        public GeneralSettingsPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            settingsModel = await SettingsService.ReadSettingsFileAsync(Content);

            if (settingsModel != null)
            {
                ConfidentialInfoBarMessageToggleSwitch.IsOn = settingsModel.IsConfidentialInfoBarEnabled;

                switch (settingsModel.AppTheme)
                {
                    case SettingsModel.Theme.Default:
                        ThemeSelector.SelectedIndex = 0;
                        break;
                    case SettingsModel.Theme.Light:
                        ThemeSelector.SelectedIndex = 1;
                        break;
                    case SettingsModel.Theme.Dark:
                        ThemeSelector.SelectedIndex = 2;
                        break;
                }

                ShowWelcomePageCheckBox.IsChecked = settingsModel.DisplayShowWelcomePageOnStartup;
            }
        }

        private void ConfidentialInfoBarMessageToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch senderToggle = sender as ToggleSwitch;
            settingsModel.IsConfidentialInfoBarEnabled = senderToggle.IsOn;
            Task.Run(() => SettingsService.UpdateSettingsFileJson(settingsModel));
        }

        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox senderComboBox = sender as ComboBox;
            if (settingsModel != null)
            {
                switch (senderComboBox.SelectedIndex)
                {
                    case 0:
                        settingsModel.AppTheme = SettingsModel.Theme.Default;
                        break;
                    case 1:
                        settingsModel.AppTheme = SettingsModel.Theme.Light;
                        break;
                    case 2:
                        settingsModel.AppTheme = SettingsModel.Theme.Dark;
                        break;
                }
                Task.Run(() => SettingsService.UpdateSettingsFileJson(settingsModel));
            }
        }

        private void ShowWelcomePageCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox senderCheckBox = sender as CheckBox;
            if (settingsModel != null)
            {
                settingsModel.DisplayShowWelcomePageOnStartup = true;
                Task.Run(() => SettingsService.UpdateSettingsFileJson(settingsModel));
                txtInfoBarSetting.Text += " (Requires Restart)";
            }
        }

        private void ShowWelcomePageCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox senderCheckBox = sender as CheckBox;
            if (settingsModel != null)
            {
                settingsModel.DisplayShowWelcomePageOnStartup = false;
                Task.Run(() => SettingsService.UpdateSettingsFileJson(settingsModel));
                txtInfoBarSetting.Text += " (Requires Restart)";
            }
        }
    }
}
