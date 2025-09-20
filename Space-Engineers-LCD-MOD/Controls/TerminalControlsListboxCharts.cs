using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.ModAPI;

namespace Space_Engineers_LCD_MOD.Controls
{
    public abstract class TerminalControlsListboxCharts : TerminalControlsCharts
    {
        public List<MyTerminalControlListBoxItem> Selection { get; protected set; }
        public void Setter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> selection) => Selection =  selection;
    }
}