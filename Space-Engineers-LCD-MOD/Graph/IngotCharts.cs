using System.Collections.Generic;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("IngotCharts", "DisplayName_BlueprintClass_Ingots")]
    public class IngotCharts : ItemCharts
    {
        public override Dictionary<MyItemType, double> ItemSource => GridLogic?.Ingots;
        public override string Title { get; protected set; } = "DisplayName_BlueprintClass_Ingots";
        public IngotCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        { }
    }
}
