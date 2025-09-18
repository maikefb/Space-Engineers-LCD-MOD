using System.Collections.Generic;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("SeedCharts", "DisplayName_BlueprintClass_GardenItems")]
    public class SeedCharts : ItemCharts
    {
        public override Dictionary<MyItemType, double> ItemSource => GridLogic?.Seeds;
        public override string Title { get; protected set; } = "DisplayName_BlueprintClass_GardenItems";
        public SeedCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }
    }
}
