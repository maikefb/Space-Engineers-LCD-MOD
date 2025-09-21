namespace Space_Engineers_LCD_MOD.Controls.Filter
{
    public abstract class TerminalControlFilter : TerminalControlsWrapper
    {
        protected override string[] VisibleForScripts { get; } = { "InventoryCharts", "BlueprintDiagram" };
    }
}