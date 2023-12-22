using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.Storage;

namespace Studiofy.Common.Service
{
    public class FileService
    {
        private class FileTag
        {
            public string FileName { get; set; }

            public string FilePath { get; set; }
        }

        public Task<TabViewItem> OpenFileAsync(StorageFile storageFile, object content)
        {
            if (storageFile != null)
            {
                return Task.FromResult(new TabViewItem()
                {
                    Header = storageFile.DisplayName,
                    Tag = new FileTag()
                    {
                        FileName = storageFile.Name,
                        FilePath = storageFile.Path
                    },
                    Content = content
                });
            }
            return null;
        }
    }
}
