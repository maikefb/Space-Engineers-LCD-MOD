using System.Linq;
using Graph.Data.Scripts.Graph;
using Graph.Data.Scripts.Graph.Sys;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Controls.Filter.Listbox;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;

namespace Space_Engineers_LCD_MOD.Controls.Filter.Buttons
{
    public sealed class ButtonBlockRemoveFromSelection : TerminalControlFilterButton
    {
        public ButtonBlockRemoveFromSelection(TerminalControlsListbox sourceList,
            TerminalControlsListbox targetList) : base(sourceList,  targetList)
        {
            CreateButton("ItemChartRemoveBlockFromSelection","EventControllerBlock_RemoveBlocks_Title" );
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
                RemoveItems(config);
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

            if (config.SelectedGroups.Length > 0)
                config.SelectedGroups = config.SelectedGroups.Where(a => !groups.Contains(a)).ToArray();
        }

        void RemoveItems(ScreenConfig config)
        {
            var ids = TargetList.Selection
                .Where(a => a.UserData is long)
                .Select(a => (long)a.UserData);

            if (config.SelectedBlocks.Length > 0)
                config.SelectedBlocks = config.SelectedBlocks.Where(a => !ids.Contains(a)).ToArray();
        }
    }
}