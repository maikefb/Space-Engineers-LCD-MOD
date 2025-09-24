using System.Collections.Generic;
using System.Linq;
using Graph.Helpers;
using Graph.System.Config;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage;
using VRage.ModAPI;
using VRage.Utils;

namespace Graph.System.TerminalControls.Filter.Listbox
{
    public sealed class ListboxItemsSelected : TerminalControlsListbox
    {
        protected override string[] VisibleForScripts => InventoryOnlyVisibility;
        
        public ListboxItemsSelected()
        {
            CreateListbox("SelectedItems", "BlockPropertyTitle_ConveyorSorterFilterItemsList");
        }

        protected override void Getter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> itemsList,
            List<MyTerminalControlListBoxItem> _)
        {
            var screenSettings = ConfigManager.GetConfigForCurrentScreen(b);

            if (screenSettings == null)
                return;

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