using Graph.System.Config;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace Graph.System.TerminalControls.Generic
{
    public class SwitchToggleLines : TerminalControlsWrapper
    {
        protected override string[] VisibleForScripts { get; } = { "InventoryCharts", "BlueprintDiagram" };
        public override IMyTerminalControl TerminalControl { get; }

        public SwitchToggleLines()
        {
            var slider = CreateControl<IMyTerminalControlOnOffSwitch>("LinesSwitch");
            slider.Getter = Getter;
            slider.Setter = Setter;
            slider.Visible = Visible;
            slider.Title = MyStringId.GetOrCompute("SafeZone_Texture_Lines");
            slider.OnText = MyStringId.GetOrCompute("HudInfoOn");
            slider.OffText = MyStringId.GetOrCompute("HudInfoOff");
            
            TerminalControl = slider;
        }

        void Setter(IMyTerminalBlock block, bool value)
        {
            var config = ConfigManager.GetConfigForCurrentScreen(block);
            if (config == null)
                return;

            config.DrawLines = value;
            ConfigManager.Sync(block);
        }

        bool Getter(IMyTerminalBlock myTerminalBlock)
        {
            var config = ConfigManager.GetConfigForCurrentScreen(myTerminalBlock);
            return config != null && config.DrawLines;
        }
    }
}