using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Space_Engineers_LCD_MOD.Controls
{
    public class SeparatorFilter : TerminalControlsCharts
    {
        public override IMyTerminalControl TerminalControl { get; } = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyTerminalBlock>("ChartFilterSeparator");
    }
}