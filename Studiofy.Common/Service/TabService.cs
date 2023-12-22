using Microsoft.UI.Xaml.Controls;

namespace Studiofy.Common.Service
{
    public class TabService
    {
        private TabView _tabView { get; set; }

        public void Set(TabView tabView)
        {
            _tabView = tabView;
        }

        public void Add(TabViewItem tabItem)
        {
            if (_tabView != null)
            {
                _tabView.TabItems.Add(tabItem);
                _tabView.SelectedItem = tabItem;
            }
        }

        public void Remove(TabViewItem tabItem)
        {
            if (_tabView != null)
            {
                _tabView.TabItems.Remove(tabItem);
            }
        }
    }
}
