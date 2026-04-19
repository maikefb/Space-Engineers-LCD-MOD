using Graph.Apps.Antenna;
using Graph.Apps.Inventory;
using Graph.Apps.Percentage;
using Graph.Apps.Refinery;

namespace Graph.System.TerminalControls.Filter
{
    public abstract class TerminalControlFilter : TerminalControlsWrapper
    {
        protected override string[] VisibleForScripts => DefaultVisibility;
        protected string[] InventoryOnlyVisibility { get; } = { InventoryLcdSurfaceScript.ID };
        protected string[] DefaultVisibility { get; } = { InventoryLcdSurfaceScript.ID, RefineryQueueSurfaceScript.ID, ProjectorLcdSurfaceScript.ID, CargoFilledSurfaceScript.ID, AntennaSurfaceScript.ID };
    }
}