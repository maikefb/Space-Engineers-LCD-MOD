using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Graph;
using Space_Engineers_LCD_MOD.Graph.Config;
using Space_Engineers_LCD_MOD.Graph.Sys;
using VRage;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Controls.Filter.Listbox
{
    public sealed class ListboxBlockSelected : TerminalControlsListbox
    {
        public ListboxBlockSelected()
        {
            CreateListbox("SelectedBlocks", "EventControllerBlock_SelectedBlocks_Title");
        }

        protected override void Getter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> blockList,
            List<MyTerminalControlListBoxItem> _)
        {
            var screenSettings = ConfigManager.GetConfigForCurrentScreen(b);

            if (screenSettings == null)
                return;

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
                            MyStringId.GetOrCompute(
                                MyTexts.GetString(MyStringId.Get("EventControllerBlock_UnknownBlock"))),
                            MyStringId.GetOrCompute(string.Format(
                                MyTexts.GetString(MyStringId.Get("EventControllerBlock_UnknownBlockTooltip")),
                                block.EntityId)),
                            block.EntityId));
                    }
                }
            }
        }
    }
}