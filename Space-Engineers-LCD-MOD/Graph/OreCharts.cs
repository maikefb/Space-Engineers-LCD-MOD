using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Graph.Data.Scripts.Graph.Sys;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("OreCharts", "RadialMenuGroupTitle_VoxelOres")]
    public class OreCharts : ItemCharts
    {
        public override Dictionary<MyItemType, double> ItemSource => GridLogic?.Ores;
        public override string Title { get; protected set; } = "RadialMenuGroupTitle_VoxelOres";

        public OreCharts(IMyTextSurface surface, VRage.Game.ModAPI.Ingame.IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        { }
    }
}
