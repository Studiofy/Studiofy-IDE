using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task PopulateFileView(StorageFolder folder, IList<TreeViewNode> nodes, XamlRoot root)
        {
            try
            {
                // Clear existing nodes in the TreeView
                nodes.Clear();
            }
            catch (NullReferenceException ex)
            {
                ContentDialog errorDialog = new()
                {
                    Title = $"{ex.Source} throws an Exception",
                    Content = ex.Message,
                    DefaultButton = ContentDialogButton.Close,
                    CloseButtonText = "Close",
                    XamlRoot = root
                };

                await errorDialog.ShowAsync();
            }

            List<IStorageItem> items = (await folder.GetItemsAsync()).ToList();

            foreach (IStorageItem item in items)
            {
                TreeViewNode newNode = new() { Content = item.Name };

                if (item is StorageFolder)
                {
                    newNode.HasUnrealizedChildren = true;
                }

                nodes.Add(newNode);

                // If the item is a folder, recursively populate its children
                if (item is StorageFolder subFolder)
                {
                    await PopulateFileView(subFolder, newNode.Children, root);
                }
            }
        }
    }
}
