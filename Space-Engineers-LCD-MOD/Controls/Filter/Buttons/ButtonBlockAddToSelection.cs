using System.Linq;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Controls.Filter.Listbox;
using Space_Engineers_LCD_MOD.Graph;
using Space_Engineers_LCD_MOD.Graph.Config;
using Space_Engineers_LCD_MOD.Graph.Sys;
using VRage;

namespace Space_Engineers_LCD_MOD.Controls.Filter.Buttons
{
    public sealed class ButtonBlockAddToSelection : TerminalControlFilterButton
    {
        public ButtonBlockAddToSelection(TerminalControlsListbox sourceList,
            TerminalControlsListbox targetList) : base(sourceList, targetList)
        {
            CreateButton("ItemChartAddBlockToSelection", "EventControllerBlock_AddBlocks_Title");
        }


        protected override void Action(IMyTerminalBlock block)
        {
            if (SourceList.Selection == null || SourceList.Selection.Count <= 0)
                return;

            var index = GetThisSurfaceIndex(block);
            var settings = ConfigManager.GetConfigForBlock(block);

            if (settings != null && settings.Screens.Count > index)
            {
                AddBlocks(settings.Screens[index]);
                AddGroups(settings.Screens[index]);
                
                SourceList.TerminalControl.UpdateVisual();
                TargetList.TerminalControl.UpdateVisual();
                ConfigManager.Sync(block, settings);
            }

            SourceList.Selection.Clear();
        }

        void AddGroups(ScreenConfig config)
        {
            var groups = SourceList.Selection.Where(a => a.UserData is string)
                .Select(a => (string)a.UserData);

            if (config.SelectedGroups.Length > 0)
            {
                var list = config.SelectedGroups.ToList();
                list.AddRange(groups);
                config.SelectedGroups = list.ToArray();
            }
            else
            {
                config.SelectedGroups = groups.ToArray();
            }
        }

        void AddBlocks(ScreenConfig config)
        {
            var ids = SourceList.Selection
                .Where(a => a.UserData is long)
                .Select(a => (long)a.UserData);

            if (config.SelectedBlocks.Length > 0)
            {
                var list = config.SelectedBlocks.ToList();
                list.AddRange(ids);
                config.SelectedBlocks = list.ToArray();
            }
            else
            {
                config.SelectedBlocks = ids.ToArray();
            }
        }
    }
}