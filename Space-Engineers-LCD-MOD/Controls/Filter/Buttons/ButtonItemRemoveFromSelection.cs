using System.Linq;
using Graph.Data.Scripts.Graph;
using Graph.Data.Scripts.Graph.Sys;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Controls.Filter.Listbox;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;
using VRage.Game;

namespace Space_Engineers_LCD_MOD.Controls.Filter.Buttons
{
    public sealed class ButtonItemRemoveFromSelection : TerminalControlFilterButton
    {
        protected override string[] VisibleForScripts => InventoryOnlyVisibility;
        
        public ButtonItemRemoveFromSelection(TerminalControlsListbox sourceList, TerminalControlsListbox targetList) :
            base(sourceList, targetList)
        {
            CreateButton("ItemChartRemoveItemFromSelection", "BlockPropertyTitle_ConveyorSorterRemove");
        }

        protected override void Action(IMyTerminalBlock b)
        {
            if (TargetList.Selection == null || TargetList.Selection.Count <= 0)
                return;

            var index = GetThisSurfaceIndex(b);
            MyTuple<int, ScreenProviderConfig> settings;
            if (ChartBase.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
            {
                var config = settings.Item2.Screens[index];
                RemoveGroups(config);
                RemoveBlocks(config);
                settings.Item2.Dirty = true;
                SourceList.TerminalControl.UpdateVisual();
                TargetList.TerminalControl.UpdateVisual();
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