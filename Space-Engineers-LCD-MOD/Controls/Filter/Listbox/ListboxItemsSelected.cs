using System.Collections.Generic;
using System.Linq;
using Graph.Data.Scripts.Graph;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Graph.Config;
using Space_Engineers_LCD_MOD.Helpers;
using VRage;
using VRage.ModAPI;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Controls.Filter.Listbox
{
    public sealed class ListboxItemsSelected : TerminalControlsListbox
    {
        protected override string[] VisibleForScripts => InventoryOnlyVisibility;
        
        public ListboxItemsSelected()
        {
            CreateListbox("SelectedItems", "BlockPropertyTitle_ConveyorSorterFilterItemsList");
        }

        protected override void Getter(IMyTerminalBlock blocks, List<MyTerminalControlListBoxItem> itemsList,
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