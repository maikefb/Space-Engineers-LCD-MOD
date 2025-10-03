using System.Text;
using Graph.System.Config;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace Graph.System.TerminalControls.Generic
{
    public sealed class SliderChartScale : TerminalControlsWrapper
    {
        public override IMyTerminalControl TerminalControl { get; }

        public SliderChartScale()
        {
            var slider = CreateControl<IMyTerminalControlSlider>("ScaleSlider");
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
            arg2.Append(Getter(b).ToString("0.000"));
        }

        void Setter(IMyTerminalBlock block, float value)
        {
            var config = ConfigManager.GetConfigForCurrentScreen(block);
            if (config == null)
                return;

            config.Scale = value;
            ConfigManager.Sync(block);
        }

        float Getter(IMyTerminalBlock block)
        {
            var config = ConfigManager.GetConfigForCurrentScreen(block);
            if (config == null)
                return 1;

            return config.Scale;
        }
    }
}