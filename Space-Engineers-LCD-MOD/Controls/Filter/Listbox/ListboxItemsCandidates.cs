using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Graph.Config;
using Space_Engineers_LCD_MOD.Graph.Sys;
using Space_Engineers_LCD_MOD.Helpers;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Controls.Filter.Listbox
{
    public sealed class ListboxItemsCandidates : TerminalControlsListbox
    {

        protected override string[] VisibleForScripts => InventoryOnlyVisibility;
        
        public ListboxItemsCandidates()
        {
            CreateListbox("CandidatesItems", "BlockPropertyTitle_ConveyorSorterCandidatesList");
        }

        protected override void Getter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> blockList,
            List<MyTerminalControlListBoxItem> _)
        {
            var screenSettings = ConfigManager.GetConfigForCurrentScreen(b);

            if (screenSettings == null)
                return;
            
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

        public bool WhiteList(object a)
        {
            var item = a as MyPhysicalItemDefinition;
            
            if(item == null)
                return false;

            var id = item.Id.ToString();
            if(id.Contains("_TreeObject/") || id.Contains("GunObject/GoodAIReward") || id.Contains("GunObject/CubePlacerItem") )
                return false;
            
            return true;

        }
    }
}