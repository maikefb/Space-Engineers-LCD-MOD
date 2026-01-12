using Graph.Charts;

namespace Graph.System.TerminalControls.Filter
{
    public abstract class TerminalControlFilter : TerminalControlsWrapper
    {
        protected override string[] VisibleForScripts => DefaultVisibility;
        protected string[] InventoryOnlyVisibility { get; } = { InventoryCharts.ID };
        protected string[] DefaultVisibility { get; } = { InventoryCharts.ID, BlueprintDiagram.ID, ContainerGraph.ID };
    }
}