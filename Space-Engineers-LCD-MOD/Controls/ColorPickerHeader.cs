using Graph.Data.Scripts.Graph;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRage.Utils;
using VRageMath;

namespace Space_Engineers_LCD_MOD.Controls
{
    public class ColorPickerHeader : TerminalControlsCharts
    {
        public override IMyTerminalControl TerminalControl => _colorPicker;
        IMyTerminalControlColor _colorPicker;

        public ColorPickerHeader()
        {
            _colorPicker = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, IMyTerminalBlock>(  "ItemChartHeaderPanel");
            _colorPicker.Getter = Getter;
            _colorPicker.Setter = Setter;
            _colorPicker.Visible = Visible;
            _colorPicker.Title = MyStringId.GetOrCompute("BlockPropertyTitle_TextPanelPublicTitle");
        }
        
        public void Setter(IMyTerminalBlock b, Color c)
        {
            var index = GetThisSurfaceIndex(b);
            if (index == -1) return;
            MyTuple<int, ScreenProviderConfig> settings;
            if (ChartBase.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
            {
                settings.Item2.Screens[index].HeaderColor = c;
                settings.Item2.Dirty = true;
            }
        }

        public Color Getter(IMyTerminalBlock b)
        {
            var index = GetThisSurfaceIndex(b);

            MyTuple<int, ScreenProviderConfig> settings;
            if (ChartBase.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
                return settings.Item2.Screens[index].HeaderColor;


            return Color.White;
        }
    }
}