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
    public class ListboxItemsSelected : TerminalControlsListboxCharts
    {
        public override IMyTerminalControl TerminalControl => _itemsListbox;
        IMyTerminalControlListbox _itemsListbox;

        public ListboxItemsSelected()
        {
            _itemsListbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyTerminalBlock>(
                "ItemChartItemsPanel");
            _itemsListbox.ListContent = Getter;
            _itemsListbox.ItemSelected = Setter;
            _itemsListbox.Visible = Visible;
            _itemsListbox.VisibleRowsCount = 8;
            _itemsListbox.Multiselect = true;
            _itemsListbox.Title = MyStringId.GetOrCompute("BlockPropertyTitle_ConveyorSorterFilterItemsList");
        }

        public void Getter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> itemsList,
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

            itemsList.AddRange(screenSettings.SelectedCategories
                .Select(g => new MyTerminalControlListBoxItem(
                    MyStringId.GetOrCompute(ItemCategoryHelper.GetGroupName(g)),
                    MyStringId.NullOrEmpty,
                    g)));
            
            foreach (var item in screenSettings.SelectedItems)
            {
                string name = null;
                string desc = null;

                var itemdef = MyDefinitionManager.Static.TryGetPhysicalItemDefinition(item);

                if (itemdef.DisplayNameEnum != null)
                    name = MyTexts.GetString(itemdef.DisplayNameEnum.Value);
                
                if (itemdef.DescriptionEnum != null)
                    desc = MyTexts.GetString(itemdef.DescriptionEnum.Value);

                if (string.IsNullOrEmpty(name)) 
                    name = $"@{item}@";
                
                if (string.IsNullOrEmpty(desc)) 
                    desc = $"@{item}@";

                itemsList.Add(new MyTerminalControlListBoxItem(
                    MyStringId.GetOrCompute(name),
                    MyStringId.GetOrCompute(desc),
                    item));
            }
        }
    }
}