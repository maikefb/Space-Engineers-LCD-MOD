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
        
        readonly List<IMyCubeBlock> _blocks = new List<IMyCubeBlock>();
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

        public void Getter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> blockList, List<MyTerminalControlListBoxItem> _)
        {
            _blocks.Clear();
            
            var index = GetThisSurfaceIndex(b);
            MyTuple<int, ScreenProviderConfig> settings;
            if (!ChartBase.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
                return;

            var screenSettings = settings.Item2.Screens[index];
            
            if(!screenSettings.SelectedBlocks.Any())
                return;

            foreach (var block in screenSettings.SelectedBlocks)
            {
                var entity = MyAPIGateway.Entities.GetEntityById(block) as IMyCubeBlock;
                if(entity != null)
                    _blocks.Add(entity);
            }

            var terminal = _blocks.Select(a => new MyTerminalControlListBoxItem(
                MyStringId.GetOrCompute(a.DisplayNameText),
                MyStringId.GetOrCompute(a.CubeGrid.DisplayName + " => " + a.DisplayNameText),
                a.EntityId));

            blockList.AddRange(terminal);
        }
    }
}