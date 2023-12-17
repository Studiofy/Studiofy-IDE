using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace WindowsCode.Studio.Services
{
    public class TabService
    {
        private TabView _tabView { get; set; }

        private TabViewItem _tabItem { get; set; }

        public TabService(TabView tabView)
        {
            _tabView = tabView;
        }

        public TabView GetTabView()
        {
            return _tabView ?? null;
        }

        public string GetTabHeader(TabViewItem selectedTab)
        {
            if (_tabView != null)
            {
                return selectedTab.Header.ToString();
            }
            return string.Empty;
        }

        public object GetFileContent(object content)
        {
            return content ?? null;
        }

        public TabViewItem CreateTabItem(string tabHeader, object contents)
        {
            if (_tabView != null)
            {
                _tabItem = new()
                {
                    Header = tabHeader,
                    Content = contents,
                    IconSource = new SymbolIconSource()
                    {
                        Symbol = Symbol.Document
                    }
                };
                _tabView.TabItems.Add(_tabItem);
                return _tabItem;
            }
            return null;
        }

        public Task<TabViewItem> CreateTabItemAsync(string tabHeader, object content, IconSource icon)
        {
            if (_tabView != null)
            {
                _tabItem = new()
                {
                    Header = tabHeader,
                    Content = content,
                    IconSource = icon
                };
                _tabView.TabItems.Add(_tabItem);
                return Task.FromResult(_tabItem);
            }
            return null;
        }
    }
}
