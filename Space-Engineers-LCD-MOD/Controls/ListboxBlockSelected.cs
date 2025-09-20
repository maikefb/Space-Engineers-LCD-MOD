using System;
using System.Collections.Generic;
using System.Linq;
using Graph.Data.Scripts.Graph;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Graph.Config;
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
    public class ListboxBlockSelected : TerminalControlsCharts
    {
        public override IMyTerminalControl TerminalControl => _selectedListbox;
        IMyTerminalControlListbox _selectedListbox;
        public List<MyTerminalControlListBoxItem> Selection;

        public ListboxBlockSelected()
        {
            _selectedListbox =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyTerminalBlock>(
                    "ItemChartSelectedPanel");
            _selectedListbox.ListContent = Getter;
            _selectedListbox.ItemSelected = Setter;
            _selectedListbox.Visible = Visible;
            _selectedListbox.VisibleRowsCount = 8;
            _selectedListbox.Multiselect = true;
            _selectedListbox.Title = MyStringId.GetOrCompute("EventControllerBlock_SelectedBlocks_Title");
        }

        public void Setter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> selection)
        {
            Selection = selection;
        }

        public void Getter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> blockList,
            List<MyTerminalControlListBoxItem> _)
        {
            var index = GetThisSurfaceIndex(b);
            MyTuple<int, ScreenProviderConfig> settings;
            if (!ChartBase.ActiveScreens.TryGetValue(b, out settings)
                || settings.Item2 == null
                || settings.Item2.Screens == null
                || settings.Item2.Screens.Count <= index
                || index < 0)
            {
                return;
            }

            var screenSettings = settings.Item2.Screens[index];

            blockList.AddRange(screenSettings.SelectedGroups.Select(a => new MyTerminalControlListBoxItem(
                MyStringId.GetOrCompute($"*{a}*"),
                MyStringId.GetOrCompute($"{MyStringId.GetOrCompute("Terminal_GroupTitle")} {a}"),
                a)));

            if (!screenSettings.SelectedBlocks.Any())
                return;

            foreach (var id in screenSettings.SelectedBlocks)
            {
                var block = MyAPIGateway.Entities.GetEntityById(id) as IMyCubeBlock;
                if (block != null)
                {
                    if (block.CubeGrid.Equals(b.CubeGrid))
                    {
                        blockList.Add(new MyTerminalControlListBoxItem(
                            MyStringId.GetOrCompute(block.DisplayNameText),
                            MyStringId.GetOrCompute(block.DisplayNameText),
                            block.EntityId));
                    }
                    else if (block.CubeGrid.IsInSameLogicalGroupAs(b.CubeGrid))
                    {
                        blockList.Add(new MyTerminalControlListBoxItem(
                            MyStringId.GetOrCompute($"@{block.DisplayNameText}@"),
                            MyStringId.GetOrCompute(block.CubeGrid.DisplayName + " => " + block.DisplayNameText),
                            block.EntityId));
                    }
                    else
                    {
                        blockList.Add(new MyTerminalControlListBoxItem(
                            MyStringId.GetOrCompute(MyTexts.GetString(MyStringId.Get("EventControllerBlock_UnknownBlock"))),
                            MyStringId.GetOrCompute(string.Format(MyTexts.GetString(MyStringId.Get("EventControllerBlock_UnknownBlockTooltip")), 
                                block.EntityId)),
                            block.EntityId));
                    }
                }
            }
        }
    }
}