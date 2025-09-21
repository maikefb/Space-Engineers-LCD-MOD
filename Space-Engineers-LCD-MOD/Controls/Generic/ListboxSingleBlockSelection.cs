using System.Collections.Generic;
using System.Linq;
using Graph.Data.Scripts.Graph;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;
using VRage.ModAPI;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Controls.Generic
{
    public class ListboxSingleBlockSelection<T> : TerminalControlsWrapper where T : class, IMyTerminalBlock
    {
        public override IMyTerminalControl TerminalControl => _terminalControl;
        IMyTerminalControl _terminalControl;
        
        readonly List<T> _reference = new List<T>();
        
        protected void CreateListbox(string id, string title)
        {
            var listbox = CreateControl<IMyTerminalControlListbox>(id);
            listbox.ListContent = Getter;
            listbox.ItemSelected = Setter;
            listbox.Visible = Visible;
            listbox.VisibleRowsCount = 8;
            listbox.Multiselect = false;
            listbox.Title = MyStringId.GetOrCompute(title);
            _terminalControl = listbox;
        }

        void Setter(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> selection)
        {
            var index = GetThisSurfaceIndex(block);
            MyTuple<int, ScreenProviderConfig> settings;

            if (ChartBase.ActiveScreens == null ||
                !ChartBase.ActiveScreens.TryGetValue(block, out settings)
                || settings.Item2?.Screens == null
                || settings.Item2.Screens.Count <= index
                || index < 0
                || selection == null
                || selection.Count != 1)
                return;

            settings.Item2.Screens[index].ReferenceBlock = selection.First().UserData as long? ?? 0;

        }

        void Getter(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> blockList,
            List<MyTerminalControlListBoxItem> selected)
        {
            var index = GetThisSurfaceIndex(block);
            MyTuple<int, ScreenProviderConfig> settings;

            if (ChartBase.ActiveScreens == null ||
                !ChartBase.ActiveScreens.TryGetValue(block, out settings)
                || settings.Item2?.Screens == null
                || settings.Item2.Screens.Count <= index
                || index < 0)
                return;
            
            _reference.Clear();
            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(block.CubeGrid)?.GetBlocksOfType(_reference);

            if (!_reference.Any())
                return;

            blockList.AddRange(_reference.Select(a => new MyTerminalControlListBoxItem(
                MyStringId.GetOrCompute(a.CustomName),
                MyStringId.GetOrCompute(a.CubeGrid.DisplayName),
                a.EntityId)));
            
            selected.Clear();
            
            var selection = blockList.FirstOrDefault(a => 
                (a.UserData as long? ?? 0) == settings.Item2.Screens[index].ReferenceBlock);
            
            if(selection != null)
                selected.Add(selection);
        }
    }
}