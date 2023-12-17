using Microsoft.UI.Xaml.Controls;
using WindowsCode.Studio.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WindowsCode.Studio.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WhatsNewPage : Page
    {
        public WhatsNewPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            AppRelease.Text = await new UpdateService().GetUpdateDescription();
        }
    }
}
