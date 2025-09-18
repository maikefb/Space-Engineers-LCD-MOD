using System;
using System.Collections.Generic;
using System.Linq;
using Graph.Data.Scripts.Graph;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Extensions;
using Space_Engineers_LCD_MOD.Graph.Config;
using Space_Engineers_LCD_MOD.Helpers;
using SpaceEngineers.Game.EntityComponents.Blocks.Events;
using VRage;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;


namespace Space_Engineers_LCD_MOD.Controls
{
    public class ListboxBlockSelection : TerminalControlsCharts
    {
        public override IMyTerminalControl TerminalControl => _selectorListbox;
        IMyTerminalControlListbox _selectorListbox;


        readonly List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();
        readonly List<IMySlimBlock> _blocks = new List<IMySlimBlock>();
        public List<MyTerminalControlListBoxItem> Selection;

        public ListboxBlockSelection()
        {
            _selectorListbox =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyTerminalBlock>(
                    "ItemChartSelectionPanel");
            _selectorListbox.ListContent = Getter;
            _selectorListbox.ItemSelected = Setter;
            _selectorListbox.Visible = Visible;
            _selectorListbox.VisibleRowsCount = 8;
            _selectorListbox.Multiselect = true;
            _selectorListbox.Title = MyStringId.GetOrCompute("ScreenTerminalInventory_FilterGamepadHelp_AllInventories");
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
            if (!ChartBase.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
                return;

            var screenSettings = settings.Item2.Screens[index];

            _grids.Clear();

            var referenceGrid = b.CubeGrid;

            MyAPIGateway.GridGroups.GetGroup(referenceGrid, GridLinkTypeEnum.Logical, _grids);

            _grids.Sort((g1, g2) =>
                (g1.GetPosition() - referenceGrid.GetPosition()).LengthSquared().CompareTo(
                    (g2.GetPosition() - referenceGrid.GetPosition()).LengthSquared()));

            foreach (var grid in _grids)
            {
                grid.GetBlocks(_blocks, block =>
                {
                    var fat = block?.FatBlock;
                    return fat != null && // Check if is a Terminal block
                           fat.HasInventory && // Checking block that have inventory
                           fat.GetUserRelationToOwner(b.OwnerId) <=
                           MyRelationsBetweenPlayerAndBlock.FactionShare && // Checking if it has access
                           !screenSettings.SelectedBlocks.Contains(fat.EntityId); // Block isn't already selected
                });
                
                blockList.AddRange(_blocks.Select(a => new MyTerminalControlListBoxItem(
                    MyStringId.GetOrCompute($"{a.FatBlock.DisplayNameText}"),
                    MyStringId.GetOrCompute(a.FatBlock.CubeGrid.DisplayName + " => " + a.FatBlock.DisplayNameText),
                    a.FatBlock.EntityId)));
                
                _blocks.Clear();
            }
        }
    }
}