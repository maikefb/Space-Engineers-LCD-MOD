using Graph.Apps.Antenna;
using Graph.Apps.Inventory;

namespace Graph.System.TerminalControls.Filter
{
    public abstract class TerminalControlFilter : TerminalControlsWrapper
    {
        protected override string[] VisibleForScripts => DefaultVisibility;
        protected string[] InventoryOnlyVisibility { get; } = { InventoryLcdSurfaceScript.ID };
        protected string[] DefaultVisibility { get; } = { InventoryLcdSurfaceScript.ID, ProjectorLcdSurfaceScript.ID, CargoFilledSurfaceScript.ID, AntennaSurfaceScript.ID };
    }
}