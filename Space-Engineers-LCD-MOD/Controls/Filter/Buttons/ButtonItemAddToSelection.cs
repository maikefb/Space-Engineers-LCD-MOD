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
    public sealed class ButtonItemAddToSelection : TerminalControlFilterButton
    {
        public ButtonItemAddToSelection(TerminalControlsListbox sourceList,
            TerminalControlsListbox targetList) : base(sourceList, targetList)
        {
            CreateButton("ItemChartAddItemToSelection", "BlockPropertyTitle_ConveyorSorterAdd");
        }


        protected override void Action(IMyTerminalBlock b)
        {
            if (SourceList.Selection == null || SourceList.Selection.Count <= 0)
                return;

            var index = GetThisSurfaceIndex(b);
            MyTuple<int, ScreenProviderConfig> settings;

            if (ChartBase.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
            {
                AddBlocks(settings.Item2.Screens[index]);
                AddGroups(settings.Item2.Screens[index]);

                settings.Item2.Dirty = true;
                SourceList.TerminalControl.UpdateVisual();
                TargetList.TerminalControl.UpdateVisual();
            }

            SourceList.Selection.Clear();
        }

        void AddGroups(ScreenConfig config)
        {
            var groups = SourceList.Selection.Where(a => a.UserData is string)
                .Select(a => (string)a.UserData);

            if (config.SelectedCategories.Length > 0)
            {
                var list = config.SelectedCategories.ToList();
                list.AddRange(groups);
                config.SelectedCategories = list.ToArray();
            }
            else
            {
                config.SelectedCategories = groups.ToArray();
            }
        }

        void AddBlocks(ScreenConfig config)
        {
            var ids = SourceList.Selection
                .Where(a => a.UserData is MyDefinitionId)
                .Select(a => ((MyDefinitionId)a.UserData));

            if (config.SelectedItems.Length > 0)
            {
                var list = config.SelectedItems.ToList();
                list.AddRange(ids);
                config.SelectedItems = list.ToArray();
            }
            else
            {
                config.SelectedItems = ids.ToArray();
            }
        }
    }
}