using System.Collections.Generic;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("ComponentCharts", "DisplayName_BlueprintClass_Components")]
    public class ComponentCharts : ItemCharts
    {
        public override Dictionary<MyItemType, double> ItemSource => GridLogic?.Components;
        public override string Title { get; protected set; } = "DisplayName_BlueprintClass_Components";
        public ComponentCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        { }
    }
}
