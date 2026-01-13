using System.Collections.Generic;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Graph.Charts
{
    [MyTextSurfaceScript(ID, "Inventory")]
    public class InventoryCharts : ItemCharts
    {
        public const string ID = "InventoryCharts";
        public const string NAME = "Inventory";
        
        public override Dictionary<MyItemType, double> ItemSource => Config == null ? null : GridLogic?.GetItems(Config, Block as IMyTerminalBlock);

        protected override string DefaultTitle => NAME;

        public InventoryCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }
    }
}
