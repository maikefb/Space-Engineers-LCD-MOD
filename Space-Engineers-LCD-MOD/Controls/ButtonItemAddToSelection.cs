using System;
using System.Linq;
using Graph.Data.Scripts.Graph;
using Graph.Data.Scripts.Graph.Sys;
using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;
using VRage.Game;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Controls
{
    public class ButtonItemAddToSelection : TerminalControlsCharts
    {
        public override IMyTerminalControl TerminalControl => _addToListboxButton;
        IMyTerminalControlButton _addToListboxButton;
        TerminalControlsListboxCharts _sourceList;
        TerminalControlsListboxCharts _targetList;

        public ButtonItemAddToSelection(TerminalControlsListboxCharts sourceList, 
            TerminalControlsListboxCharts targetList)
        {
            _sourceList = sourceList;
            _targetList = targetList;
            _addToListboxButton =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>(
                    "ItemChartAddItemToSelection");
            _addToListboxButton.Action = BlockSelection;
            _addToListboxButton.Visible = Visible;
            _addToListboxButton.Title = MyStringId.GetOrCompute("BlockPropertyTitle_ConveyorSorterAdd");
        }


        public void BlockSelection(IMyTerminalBlock b)
        {
            if (_sourceList.Selection != null && _sourceList.Selection.Count > 0)
            {
                var index = GetThisSurfaceIndex(b);
                MyTuple<int, ScreenProviderConfig> settings;

                if (ChartBase.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
                {
                    AddBlocks(settings.Item2.Screens[index]);
                    AddGroups(settings.Item2.Screens[index]);

                    settings.Item2.Dirty = true;
                    _sourceList.TerminalControl.UpdateVisual();
                    _targetList.TerminalControl.UpdateVisual();
                }

                _sourceList.Selection.Clear();
            }
        }

        public void AddGroups(ScreenConfig config)
        {
            var groups = _sourceList.Selection.Where(a => a.UserData is string)
                .Select(a => (string)a.UserData);

            if (config.SelectedCategories.Length > 0)
            {
                var list = config.SelectedCategories.ToList();
                list.AddRange(groups);
                config.SelectedCategories = list.ToArray();
            }
            else
            {
                config.SelectedCategories = groups.ToArray();
            }
        }

        public void AddBlocks(ScreenConfig config)
        {
            var ids = _sourceList.Selection
                .Where(a => a.UserData is MyDefinitionId)
                .Select(a => ((MyDefinitionId)a.UserData));

            if (config.SelectedItems.Length > 0)
            {
                var list = config.SelectedItems.ToList();
                list.AddRange(ids);
                config.SelectedItems = list.ToArray();
            }
            else
            {
                config.SelectedItems = ids.ToArray();
            }
        }
    }
}