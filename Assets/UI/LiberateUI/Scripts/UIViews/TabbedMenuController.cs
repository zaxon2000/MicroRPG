using UnityEngine.UIElements;

namespace LiberateUI
{
    public class TabbedMenuController
    {
        private const string tabButtonClassName = "tab-button";
        private const string activeTabClassName = "tab-button--active";
        private const string unselectedContentClassName = "unselected-content";
        private const string tabNameSuffix = "-tab-button";
        private const string contentNameSuffix = "-content";

        private readonly VisualElement root;

        public TabbedMenuController(VisualElement root)
        {
            this.root = root;
        }

        public void RegisterTabCallbacks()
        {
            UQueryBuilder<VisualElement> tabs = GetAllTabs();
            tabs.ForEach(tab => tab.RegisterCallback<ClickEvent>(OnTabClick));
        }

        private void OnTabClick(ClickEvent evt)
        {
            VisualElement clickedTab = evt.currentTarget as VisualElement;

            if (!IsTabSelected(clickedTab))
            {
                GetAllTabs().Where(
                    tab => tab != clickedTab && IsTabSelected(tab)
                ).ForEach(UnselectTab);

                SelectTab(clickedTab);
            }
        }

        private bool IsTabSelected(VisualElement tab)
        {
            return tab.ClassListContains(activeTabClassName);
        }

        private UQueryBuilder<VisualElement> GetAllTabs()
        {
            return root.Query<VisualElement>(className: tabButtonClassName);
        }

        private void SelectTab(VisualElement tab)
        {
            tab.AddToClassList(activeTabClassName);

            VisualElement content = FindContent(tab);
            if (content != null)
            {
                content.RemoveFromClassList(unselectedContentClassName);
            }
        }

        private void UnselectTab(VisualElement tab)
        {
            tab.RemoveFromClassList(activeTabClassName);

            VisualElement content = FindContent(tab);
            if (content != null)
            {
                content.AddToClassList(unselectedContentClassName);
            }
        }

        private VisualElement FindContent(VisualElement tab)
        {
            string contentName = tab.name.Replace(tabNameSuffix, contentNameSuffix);
            return root.Q<VisualElement>(contentName);
        }

        public void SelectFirstTab()
        {
            VisualElement firstTab = GetAllTabs().First();
            if (firstTab != null)
            {
                GetAllTabs().Where(
                    tab => tab != firstTab && IsTabSelected(tab)
                ).ForEach(UnselectTab);

                SelectTab(firstTab);
            }
        }
    }
}