using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Controls
{
    public class LabelSeparator : TerminalControlsCharts
    {
        public override IMyTerminalControl TerminalControl { get; }

        public LabelSeparator()
        {
           var label = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyTerminalBlock>("ChartFilterLabel");
           TerminalControl = label;
           label.Label = MyStringId.GetOrCompute("ScenarioSelectionScreen_Filter");
            
        }
    }
}