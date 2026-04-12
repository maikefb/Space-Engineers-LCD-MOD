using Graph.Charts;
using Graph.System.Config;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace Graph.System.TerminalControls.Generic
{
    public class CheckboxHideEmpty : TerminalControlsWrapper
    {
        protected override string[] VisibleForScripts { get; } = { InventoryCharts.ID };
        public override IMyTerminalControl TerminalControl { get; }

        public CheckboxHideEmpty()
        {
            var slider = CreateControl<IMyTerminalControlCheckbox>("LinesSwitch");
            slider.Getter = Getter;
            slider.Setter = Setter;
            slider.Visible = Visible;
            slider.Title = MyStringId.GetOrCompute("HideEmpty");
            slider.OnText = MyStringId.GetOrCompute("HudInfoOn");
            slider.OffText = MyStringId.GetOrCompute("HudInfoOff");
            
            TerminalControl = slider;
        }

        void Setter(IMyTerminalBlock block, bool value)
        {
            var config = ConfigManager.GetConfigForCurrentScreen(block);
            if (config == null)
                return;

            config.HideEmpty = value;
            ConfigManager.Sync(block);
        }

        bool Getter(IMyTerminalBlock myTerminalBlock)
        {
            var config = ConfigManager.GetConfigForCurrentScreen(myTerminalBlock);
            return config != null && config.HideEmpty;
        }
    }
}