using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WindowsCode.Studio.Views.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewFileDialog : ContentDialog
    {
        private string _fileName;

        public NewFileDialog()
        {
            InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SetFileName(FileNameTextBox.Text);
        }

        public string GetFileName()
        {
            return _fileName ?? null;
        }

        public void SetFileName(string fileName)
        {
            _fileName = fileName;
        }
    }
}
