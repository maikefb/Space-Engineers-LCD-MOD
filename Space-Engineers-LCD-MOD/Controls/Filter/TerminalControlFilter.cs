namespace Space_Engineers_LCD_MOD.Controls.Filter
{
    public abstract class TerminalControlFilter : TerminalControlsWrapper
    {
        protected override string[] VisibleForScripts => DefaultVisibility;
        protected string[] InventoryOnlyVisibility { get; } = { "InventoryCharts" };
        protected string[] DefaultVisibility { get; } = { "InventoryCharts", "BlueprintDiagram" };
    }
}