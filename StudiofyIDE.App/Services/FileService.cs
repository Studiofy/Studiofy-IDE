using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using WindowsCode.Studio.Views.TabViews;

namespace WindowsCode.Studio.Services
{
    public class FileService
    {
        private TabService _tabService;

        private EditBoxTabView _editor;

        public TabService GetTabService()
        {
            return _tabService;
        }

        public void SetTabService(TabService tabService)
        {
            _tabService = tabService;
        }

        public EditBoxTabView GetEditor()
        {
            return _editor;
        }

        public void SetEditor(EditBoxTabView editor)
        {
            _editor = editor;
        }

        public async Task OpenSelectedFile(StorageFile storageFile, UIElement contentRoot, RichEditBox codeEditor, TabView fileTabView)
        {
            if (storageFile == null)
            {
                ContentDialog errorDialog = new()
                {
                    Title = "Error",
                    Content = "File is Null",
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close
                };
                errorDialog.XamlRoot = contentRoot.XamlRoot;
                await errorDialog.ShowAsync();
            }
            else
            {
                if (codeEditor != null && GetEditor() != null)
                {
                    string fileContent = await FileIO.ReadTextAsync(storageFile);

                    if (string.IsNullOrEmpty(fileContent))
                    {
                        ContentDialog errorDialog = new()
                        {
                            Title = "Error",
                            Content = "Cannot Add File Contents",
                            CloseButtonText = "OK",
                            DefaultButton = ContentDialogButton.Close
                        };
                        errorDialog.XamlRoot = contentRoot.XamlRoot;
                        await errorDialog.ShowAsync();
                    }
                    else
                    {
                        codeEditor.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, fileContent);
                    }
                }
                else
                {
                    ContentDialog errorDialog = new()
                    {
                        Title = "Error",
                        Content = "Cannot Read File",
                        CloseButtonText = "OK",
                        DefaultButton = ContentDialogButton.Close
                    };
                    errorDialog.XamlRoot = contentRoot.XamlRoot;
                    await errorDialog.ShowAsync();
                }

                GetTabService().CreateTabItem(storageFile.Path, GetEditor());
                fileTabView.SelectedIndex += 1;
            }
        }

        public void OpenSelectedFolder()
        {

        }
    }
}
