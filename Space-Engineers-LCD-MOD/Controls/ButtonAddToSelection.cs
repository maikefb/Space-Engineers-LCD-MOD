using System;
using System.Linq;
using Graph.Data.Scripts.Graph;
using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Controls
{
    public class ButtonAddToSelection : TerminalControlsCharts
    {
        public override IMyTerminalControl TerminalControl => _addToListboxButton;
        IMyTerminalControlButton _addToListboxButton;
        ListboxBlockSelection _sourceList;
        ListboxBlockSelected _targetList;
        
        public ButtonAddToSelection(ListboxBlockSelection sourceList, ListboxBlockSelected targetList)
        {
            _sourceList = sourceList;
            _targetList = targetList;
            _addToListboxButton =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>(
                    "ItemChartAddToSelection");
            _addToListboxButton.Action = Action;
            _addToListboxButton.Visible = Visible;
            _addToListboxButton.Title = MyStringId.GetOrCompute("EventControllerBlock_AddBlocks_Title");
        }

        
        public void Action(IMyTerminalBlock b)
        {
            if (_sourceList.Selection != null && _sourceList.Selection.Count > 0)
            {
                var index = GetThisSurfaceIndex(b);
                MyTuple<int, ScreenProviderConfig> settings;

                if (ChartBase.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
                {
                    if (settings.Item2.Screens[index].SelectedBlocks.Length == 0)
                    {
                        settings.Item2.Screens[index].SelectedBlocks =
                            _sourceList.Selection.Select(a => (long)a.UserData).ToArray();
                    }
                    else
                    {
                        var list = settings.Item2.Screens[index].SelectedBlocks.ToList();
                        list.AddRange(_sourceList.Selection.Select(a => (long)a.UserData));
                        settings.Item2.Screens[index].SelectedBlocks = list.ToArray();
                    }

                    settings.Item2.Dirty = true;
                    _sourceList.TerminalControl.UpdateVisual();
                    _targetList.TerminalControl.UpdateVisual();
                }
                
                _sourceList.Selection.Clear();
            }
        }
    }
}