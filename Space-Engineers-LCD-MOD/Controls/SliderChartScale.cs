using System.Text;
using Graph.Data.Scripts.Graph;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Controls
{
    public class SliderChartScale : TerminalControlsCharts
    {
        public override IMyTerminalControl TerminalControl { get; }

        public SliderChartScale()
        {
            var slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyTerminalBlock>(
                "ItemChartScaleSlider");
            slider.Getter = Getter;
            slider.Setter = Setter;
            slider.Visible = Visible;
            slider.SetLimits(.1f, 2.5f);
            slider.Writer = Writer;
            slider.Title = MyStringId.GetOrCompute("BlockPropertyTitle_Scale");

            TerminalControl = slider;
        }

        void Writer(IMyTerminalBlock b, StringBuilder arg2)
        {
            var index = GetThisSurfaceIndex(b);
            if (index == -1) return;
            MyTuple<int, ScreenProviderConfig> settings;
            if (ChartBase.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
            {
                arg2.Clear();
                arg2.Append(settings.Item2.Screens[index].Scale.ToString("0.000"));
            }
        }

        void Setter(IMyTerminalBlock b, float c)
        {
            var index = GetThisSurfaceIndex(b);
            if (index == -1) return;
            MyTuple<int, ScreenProviderConfig> settings;
            if (ChartBase.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
            {
                settings.Item2.Screens[index].Scale = c;
                settings.Item2.Dirty = true;
            }
        }

        float Getter(IMyTerminalBlock b)
        {
            var index = GetThisSurfaceIndex(b);

            MyTuple<int, ScreenProviderConfig> settings;
            if (ChartBase.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
                return settings.Item2.Screens[index].Scale;
            
            return 1;
        }
    }
}