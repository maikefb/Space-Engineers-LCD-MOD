using System.Collections.Generic;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Space_Engineers_LCD_MOD.Graph
{
    [MyTextSurfaceScript("InventoryCharts", "Inventory")]
    public class InventoryCharts : ItemCharts
    {
        public override Dictionary<MyItemType, double> ItemSource => Config == null ? null : GridLogic?.GetItems(Config, Block as IMyTerminalBlock);

        protected override string DefaultTitle { get; set; } = "Inventory";

        public InventoryCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }
    }
}
