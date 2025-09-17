using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("ConsumablesCharts", "DisplayName_BlueprintClass_Consumables")]
    public class ConsumablesCharts : ItemCharts
    {
        public override Dictionary<MyItemType, double> ItemSource => GridLogic?.Consumables;
        public override string Title { get; protected set; } = "DisplayName_BlueprintClass_Consumables";
        public ConsumablesCharts(IMyTextSurface surface, VRage.Game.ModAPI.Ingame.IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {}
    }
}
