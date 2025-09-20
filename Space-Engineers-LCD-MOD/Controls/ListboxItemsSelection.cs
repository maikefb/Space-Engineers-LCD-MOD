using System;
using System.Collections.Generic;
using System.Linq;
using Graph.Data.Scripts.Graph;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Graph.Config;
using Space_Engineers_LCD_MOD.Helpers;
using SpaceEngineers.Game.EntityComponents.Blocks.Events;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;


namespace Space_Engineers_LCD_MOD.Controls
{
    public class ListboxItemsSelection : TerminalControlsListboxCharts
    {
        public override IMyTerminalControl TerminalControl => _itemsListbox;
        IMyTerminalControlListbox _itemsListbox;
        
        public ListboxItemsSelection()
        {
            _itemsListbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyTerminalBlock>(
                    "ItemChartItemsPanel");
            _itemsListbox.ListContent = Getter;
            _itemsListbox.ItemSelected = Setter;
            _itemsListbox.Visible = Visible;
            _itemsListbox.VisibleRowsCount = 8;
            _itemsListbox.Multiselect = true;
            _itemsListbox.Title = MyStringId.GetOrCompute("BlockPropertyTitle_ConveyorSorterCandidatesList");
        }

        public void Getter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> blockList,
            List<MyTerminalControlListBoxItem> _)
        {
            var index = GetThisSurfaceIndex(b);
            MyTuple<int, ScreenProviderConfig> settings;

            if (ChartBase.ActiveScreens == null ||
                !ChartBase.ActiveScreens.TryGetValue(b, out settings)
                || settings.Item2?.Screens == null
                || settings.Item2.Screens.Count <= index
                || index < 0)
                return;

            var screenSettings = settings.Item2.Screens[index];
            
            blockList.AddRange(ItemCategoryHelper.Groups.Where(g => !screenSettings.SelectedCategories.Contains(g))
                .Select(g => new MyTerminalControlListBoxItem(
                MyStringId.GetOrCompute(ItemCategoryHelper.GetGroupName(g)),
                MyStringId.NullOrEmpty,
                g)));
            
            var allItems = MyDefinitionManager.Static.GetAllDefinitions().Where(WhiteList).Where(a => !screenSettings.SelectedItems.Contains(a.Id)).ToList();

            blockList.AddRange(allItems.Select(a => new MyTerminalControlListBoxItem(
                MyStringId.GetOrCompute(a.DisplayNameText),
                MyStringId.GetOrCompute(a.DescriptionText),
                a.Id)));
        }

        public bool WhiteList(MyDefinitionBase a)
        {
            var item = a as MyPhysicalItemDefinition;
            
            if(item == null)
                return false;

            var id = item.Id.ToString();
            if(id.Contains("_TreeObject/") || id.Contains("GunObject/GoodAIReward") || id.Contains("GunObject/CubePlacerItem") )
                return false;

            if(item.ToString().Contains("Tree"))
                DebuggerHelper.Break();
            
            return true;

        }
    }
}