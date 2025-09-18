using System.Collections.Generic;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("OreCharts", "RadialMenuGroupTitle_VoxelOres")]
    public class OreCharts : ItemCharts
    {
        public override Dictionary<MyItemType, double> ItemSource => GridLogic?.Ores;
        public override string Title { get; protected set; } = "RadialMenuGroupTitle_VoxelOres";

        public OreCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        { }
    }
}
