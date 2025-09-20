using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.GUI.TextPanel;

namespace Space_Engineers_LCD_MOD.Controls
{
    public abstract class TerminalControlsCharts
    {
        public int GetThisSurfaceIndex(IMyTerminalBlock block)
        {
            var multiTextPanel = block.Components.Get<MyMultiTextPanelComponent>();
            return multiTextPanel?.SelectedPanelIndex ?? 0;
        }

        public bool Visible(IMyTerminalBlock b)
        {
            var sf = ((IMyTextSurfaceProvider)b).GetSurface(GetThisSurfaceIndex(b));
            return !string.IsNullOrEmpty(sf?.Script) && sf.Script.Contains("Charts") &&
                   sf.ContentType == ContentType.SCRIPT;
        }

        public abstract IMyTerminalControl TerminalControl { get; }
    }
}