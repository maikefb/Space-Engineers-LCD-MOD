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
    [MyTextSurfaceScript("AmmoCharts", "DisplayName_BlueprintClass_Ammo")]
    public class AmmoCharts : ItemCharts
    {
        public override Dictionary<MyItemType, double> ItemSource => GridLogic?.Ammo;
        public override string Title { get; protected set; } = "DisplayName_BlueprintClass_Ammo";
        public AmmoCharts(IMyTextSurface surface, VRage.Game.ModAPI.Ingame.IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        { }
    }
}
