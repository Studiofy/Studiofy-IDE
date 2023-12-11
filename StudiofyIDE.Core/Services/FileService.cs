using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace WindowsCode.Core.Services
{
    public class FileService
    {
        private TabView _tabView;

        public FileService(TabView tabView)
        {
            _tabView = tabView;
        }

        public TabView GetDefaultTabView()
        {
            return _tabView ?? null;
        }

        public async Task NewFile(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && GetDefaultTabView() != null)
            {

            }

        }
    }
}
