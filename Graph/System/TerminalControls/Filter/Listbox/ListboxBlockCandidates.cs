using System;
using System.Collections.Generic;
using System.Linq;
using Graph.Helpers;
using Graph.System.Config;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using IMyBlockGroup = Sandbox.ModAPI.Ingame.IMyBlockGroup;

namespace Graph.System.TerminalControls.Filter.Listbox
{
    public sealed class ListboxBlockCandidates : TerminalControlsListbox
    {
        readonly List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();
        readonly List<IMySlimBlock> _blocks = new List<IMySlimBlock>();
        readonly List<IMyBlockGroup> _groups = new List<IMyBlockGroup>();

        public ListboxBlockCandidates()
        {
            CreateListbox("CandidatesBlocks", "ScreenTerminalInventory_FilterGamepadHelp_AllInventories");
        }

        protected override void Getter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> blockList,
            List<MyTerminalControlListBoxItem> selected)
        {
            var screenSettings = ConfigManager.GetConfigForCurrentScreen(b);

            if (screenSettings == null)
                return;

            _grids.Clear();
            _groups.Clear();

            var referenceGrid = b.CubeGrid;

            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(b.CubeGrid).GetBlockGroups(_groups,
                g => !screenSettings.SelectedGroups.Contains(g.Name));
            blockList.AddRange(_groups.Select(a => ListBoxItemHelper.GetOrComputeListBoxItem(
                $"*{a.Name}*",
                $"{MyStringId.GetOrCompute("Terminal_GroupTitle")} {a.Name}",
                a.Name)));

            MyAPIGateway.GridGroups.GetGroup(referenceGrid, GridLinkTypeEnum.Logical, _grids);

            _blocks.Clear();

            referenceGrid.GetBlocks(_blocks, c => IsValidBlock(c, b, screenSettings));
            blockList.AddRange(_blocks.Select(a => ListBoxItemHelper.GetOrComputeListBoxItem(
                a.FatBlock.DisplayNameText,
                a.FatBlock.DisplayNameText,
                a.FatBlock.EntityId)));

            foreach (var grid in _grids)
            {
                if (grid == b.CubeGrid)
                    continue;

                _blocks.Clear();

                grid.GetBlocks(_blocks, c => IsValidBlock(c, b, screenSettings));

                blockList.AddRange(_blocks.Select(a => ListBoxItemHelper.GetOrComputeListBoxItem(
                    $"@{a.FatBlock.DisplayNameText}@",
                    a.FatBlock.CubeGrid.DisplayName + " => " + a.FatBlock.DisplayNameText,
                    a.FatBlock.EntityId)));

                _blocks.Clear();
            }
            
            base.Getter(b, blockList, selected);
        }

        bool IsValidBlock(IMySlimBlock block, IMyTerminalBlock referenceBlock, ScreenConfig config)
        {
            var fat = block?.FatBlock;
            return fat != null && // Check if is a Terminal block
                   fat.HasInventory && // Checking block that have inventory
                   fat.GetUserRelationToOwner(referenceBlock.OwnerId) <=
                   MyRelationsBetweenPlayerAndBlock.FactionShare && // Checking if it has access
                   !config.SelectedBlocks.Contains(fat.EntityId); // Block isn't already selected
        }
    }
}