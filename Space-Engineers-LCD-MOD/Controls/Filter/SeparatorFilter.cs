using Sandbox.ModAPI.Interfaces.Terminal;

namespace Space_Engineers_LCD_MOD.Controls.Filter
{
    public class SeparatorFilter : TerminalControlFilter
    {
        public override IMyTerminalControl TerminalControl { get; }
        
        public SeparatorFilter()
        {
            var separator = CreateControl<IMyTerminalControlSeparator>("ChartFilterSeparator");
            separator.Visible = Visible;
            TerminalControl = separator;
        }
    }
}