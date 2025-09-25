namespace Graph.System.TerminalControls.Filter
{
    public abstract class TerminalControlFilter : TerminalControlsWrapper
    {
        protected override string[] VisibleForScripts => DefaultVisibility;
        protected string[] InventoryOnlyVisibility { get; } = { "InventoryCharts" };
        protected string[] DefaultVisibility { get; } = { "InventoryCharts", "BlueprintDiagram" };
    }
}