using Microsoft.UI.Xaml.Controls;
using WindowsCode.Studio.Views.TabViews;

namespace WindowsCode.Studio.Models
{
    public class FileTabViewModel
    {
        private TabViewItem _tabItem;

        public TabViewItem CreateTabItem(TabView tabView, string tabHeader)
        {
            _tabItem = new()
            {
                Header = tabHeader,
                Content = new EditBoxTabView(),
                IconSource = new SymbolIconSource()
                {
                    Symbol = Symbol.Document
                }
            };
            tabView.TabItems.Add(_tabItem);
            return _tabItem;
        }

        public TabViewItem CreateTabItem(TabView tabView, string tabHeader, object contents)
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
            tabView.TabItems.Add(_tabItem);
            return _tabItem;
        }
    }
}
