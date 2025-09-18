using System;
using System.Linq;
using Graph.Data.Scripts.Graph;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Controls
{
    public class ButtonRemoveFromSelection : TerminalControlsCharts
    {
        public override IMyTerminalControl TerminalControl => _removeFromListboxButton;
        IMyTerminalControlButton _removeFromListboxButton;
        ListboxBlockSelection _sourceList;
        ListboxBlockSelected _targetList;

        public ButtonRemoveFromSelection(ListboxBlockSelection sourceList, ListboxBlockSelected targetList)
        {
            _sourceList = sourceList;
            _targetList = targetList;

            _removeFromListboxButton =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>(
                    "ItemChartRemoveFromSelection");
            _removeFromListboxButton.Action = Action;
            _removeFromListboxButton.Visible = Visible;
            _removeFromListboxButton.Title = MyStringId.GetOrCompute("EventControllerBlock_RemoveBlocks_Title");
        }

        public void Action(IMyTerminalBlock b)
        {
            if (_targetList.Selection != null && _targetList.Selection.Count > 0)
            {
                var index = GetThisSurfaceIndex(b);
                MyTuple<int, ScreenProviderConfig> settings;
                if (ChartBase.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
                {
                    if (settings.Item2.Screens[index].SelectedBlocks.Length == 0)
                        return;

                    var array = _targetList.Selection.Select(a => (long)a.UserData);
                    settings.Item2.Screens[index].SelectedBlocks = settings.Item2.Screens[index].SelectedBlocks
                        .Where(a => !array.Contains(a)).ToArray();

                    settings.Item2.Dirty = true;
                    _sourceList.TerminalControl.UpdateVisual();
                    _targetList.TerminalControl.UpdateVisual();
                }

                _targetList.Selection.Clear();
            }
        }
    }
}