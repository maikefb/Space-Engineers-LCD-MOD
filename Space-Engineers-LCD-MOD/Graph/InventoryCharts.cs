using System.Collections.Generic;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("InventoryCharts", "Inventory")]
    public class InventoryCharts : ItemCharts
    {
        public override Dictionary<MyItemType, double> ItemSource
        {
            get
            {
                if (Config == null)
                    return null;

                return GridLogic?.GetItems(Config, Block as IMyTerminalBlock);
            }
        }

        public override string Title { get; protected set; } = "Inventory";

        public InventoryCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            
        }
    }
}
