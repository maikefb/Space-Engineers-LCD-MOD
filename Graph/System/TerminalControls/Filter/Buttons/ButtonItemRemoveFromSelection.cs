using System.Linq;
using Graph.System.Config;
using Graph.System.TerminalControls.Filter.Listbox;
using Sandbox.ModAPI;
using VRage.Game;

namespace Graph.System.TerminalControls.Filter.Buttons
{
    public sealed class ButtonItemRemoveFromSelection : TerminalControlFilterButton
    {
        protected override string[] VisibleForScripts => InventoryOnlyVisibility;
        
        public ButtonItemRemoveFromSelection(TerminalControlsListbox sourceList, TerminalControlsListbox targetList) :
            base(sourceList, targetList)
        {
            CreateButton("ItemChartRemoveItemFromSelection", "BlockPropertyTitle_ConveyorSorterRemove");
        }

        protected override void Action(IMyTerminalBlock block)
        {

            if (TargetList.Selection == null || TargetList.Selection.Count <= 0)
                return;

            var index = GetThisSurfaceIndex(block);
            var settings = ConfigManager.GetConfigForBlock(block);

            if (settings != null && settings.Screens.Count > index)
            {
                var config = settings.Screens[index];
                RemoveGroups(config);
                RemoveBlocks(config);
                SourceList.TerminalControl.UpdateVisual();
                TargetList.TerminalControl.UpdateVisual();
                ConfigManager.Sync(block, settings);
            }

            TargetList.Selection.Clear();
        }

        void RemoveGroups(ScreenConfig config)
        {
            var groups = TargetList.Selection
                .Where(a => a.UserData is string)
                .Select(a => (string)a.UserData);

            if (config.SelectedCategories.Length > 0)
                config.SelectedCategories = config.SelectedCategories.Where(a => !groups.Contains(a)).ToArray();
        }

        void RemoveBlocks(ScreenConfig config)
        {
            var ids = TargetList.Selection
                .Where(a => a.UserData is MyDefinitionId)
                .Select(a => (MyDefinitionId)a.UserData);

            if (config.SelectedItems.Length > 0)
                config.SelectedItems = config.SelectedItems.Where(a => !ids.Contains(a)).ToArray();
        }
    }
}