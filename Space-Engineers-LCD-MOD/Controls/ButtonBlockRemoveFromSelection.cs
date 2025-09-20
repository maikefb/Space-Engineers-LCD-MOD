using System;
using System.Linq;
using Graph.Data.Scripts.Graph;
using Graph.Data.Scripts.Graph.Sys;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;
using VRage.Game;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Controls
{
    public class ButtonBlockRemoveFromSelection : TerminalControlsCharts
    {
        public override IMyTerminalControl TerminalControl => _removeFromListboxButton;
        IMyTerminalControlButton _removeFromListboxButton;
        TerminalControlsListboxCharts _sourceList;
        TerminalControlsListboxCharts _targetList;

        public ButtonBlockRemoveFromSelection(TerminalControlsListboxCharts sourceList, TerminalControlsListboxCharts targetList)
        {
            _sourceList = sourceList;
            _targetList = targetList;

            _removeFromListboxButton =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>(
                    "ItemChartRemoveBlockFromSelection");
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
                    var config = settings.Item2.Screens[index];
                    RemoveGroups(config);
                    RemoveItems(config);
                    settings.Item2.Dirty = true;
                    _sourceList.TerminalControl.UpdateVisual();
                    _targetList.TerminalControl.UpdateVisual();
                }

                _targetList.Selection.Clear();
            }
        }
        
        public void RemoveGroups(ScreenConfig config)
        {
            var groups = _targetList.Selection
                .Where(a => a.UserData is string)
                .Select(a => (string)a.UserData);

            if (config.SelectedCategories.Length > 0) 
                config.SelectedCategories = config.SelectedCategories.Where(a => !groups.Contains(a)).ToArray();
        }

        public void RemoveItems(ScreenConfig config)
        {
            var ids = _targetList.Selection
                .Where(a => a.UserData is MyDefinitionId)
                .Select(a => (MyDefinitionId)a.UserData);

            if (config.SelectedItems.Length > 0) 
                config.SelectedItems = config.SelectedItems.Where(a => !ids.Contains(a)).ToArray();
        }
    }
}