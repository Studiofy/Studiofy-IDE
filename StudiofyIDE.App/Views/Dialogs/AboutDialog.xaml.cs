using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WindowsCode.Studio.Views.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutDialog : ContentDialog
    {
        public AboutDialog()
        {
            InitializeComponent();
            AppVersion.Text = Windows.ApplicationModel.Package.Current.Id.Version.Major
                        + "." + Windows.ApplicationModel.Package.Current.Id.Version.Minor
                        + "." + Windows.ApplicationModel.Package.Current.Id.Version.Build
                        + "." + Windows.ApplicationModel.Package.Current.Id.Version.Revision;
            AppPublisher.Text = Windows.ApplicationModel.Package.Current.PublisherDisplayName;
            AppLicense.Content = "Mozilla Public License Version 2.0";
            AppLicense.NavigateUri = new System.Uri("https://github.com/Studiofy/Studiofy-IDE?tab=MPL-2.0-1-ov-file");
        }
    }
}
