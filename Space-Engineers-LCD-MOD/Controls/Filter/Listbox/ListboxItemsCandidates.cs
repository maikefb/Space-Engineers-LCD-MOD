using System;
using System.Collections.Generic;
using System.Linq;
using Graph.Data.Scripts.Graph;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Graph.Config;
using Space_Engineers_LCD_MOD.Helpers;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Controls.Filter.Listbox
{
    public sealed class ListboxItemsCandidates : TerminalControlsListbox
    {
        public ListboxItemsCandidates()
        {
            CreateListbox("CandidatesItems", "BlockPropertyTitle_ConveyorSorterCandidatesList");
        }

        protected override void Getter(IMyTerminalBlock blocks, List<MyTerminalControlListBoxItem> blockList,
            List<MyTerminalControlListBoxItem> _)
        {
            var index = GetThisSurfaceIndex(blocks);
            MyTuple<int, ScreenProviderConfig> settings;

            if (ChartBase.ActiveScreens == null ||
                !ChartBase.ActiveScreens.TryGetValue(blocks, out settings)
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