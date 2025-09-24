using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Graph;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using IMyCubeBlock = VRage.Game.ModAPI.IMyCubeBlock;
using IMyCubeGrid = VRage.Game.ModAPI.IMyCubeGrid;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("ContainerCharts", "Contêineres")]
    public class ContainerGraph : ChartBase
    {
        private const float LINE = 18f;
        private const float H_MARGIN = 12f;
        private const float TOP_MARGIN = 56f;

        public ContainerGraph(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface.ContentType = ContentType.SCRIPT;
        }

        protected override string DefaultTitle => "Contêineres";
        public override Dictionary<MyItemType, double> ItemSource => null;

        public override void Run()
        {
            base.Run();
            if (Config == null) return;

            Scale = GetAutoScaleUniform();
            UpdateViewBox();

            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                DrawTitle(sprites);

                var details = new List<Entry>(128);
                AggregateAllContainersInLogicalGroup((IMyCubeGrid)Block?.CubeGrid, details);

                details.Sort((a, b) =>
                {
                    var fa = a.Cap > 0 ? a.Used / a.Cap : 0;
                    var fb = b.Cap > 0 ? b.Used / b.Cap : 0;
                    var cmp = fb.CompareTo(fa);
                    if (cmp != 0) return cmp;
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });

                var lh = LINE * Scale;
                var leftX = ViewBox.Position.X + H_MARGIN * Scale;
                var rightX = ViewBox.Position.X + ViewBox.Size.X - H_MARGIN * Scale;
                var y = ViewBox.Position.Y + TOP_MARGIN * Scale;

                if (details.Count == 0)
                    sprites.Add(new MySprite
                    {
                        Type = SpriteType.TEXT,
                        Data = "- nenhum contêiner encontrado -",
                        Position = new Vector2(leftX, y),
                        Color = Surface.ScriptForegroundColor,
                        Alignment = TextAlignment.LEFT,
                        RotationOrScale = 0.88f * Scale
                    });
                else
                    for (var i = 0; i < details.Count; i++)
                    {
                        var e = details[i];
                        var pct = 0;
                        if (e.Cap > 1e-9)
                            pct = (int)Math.Round(Math.Max(0.0, Math.Min(1.0, e.Used / e.Cap)) * 100.0);

                        sprites.Add(new MySprite
                        {
                            Type = SpriteType.TEXT,
                            Data = e.Name,
                            Position = new Vector2(leftX, y),
                            Color = Surface.ScriptForegroundColor,
                            Alignment = TextAlignment.LEFT,
                            RotationOrScale = 0.86f * Scale
                        });

                        sprites.Add(new MySprite
                        {
                            Type = SpriteType.TEXT,
                            Data = pct.ToString(CultureInfo.InvariantCulture) + "%",
                            Position = new Vector2(rightX, y),
                            Color = Surface.ScriptForegroundColor,
                            Alignment = TextAlignment.RIGHT,
                            RotationOrScale = 0.86f * Scale
                        });

                        y += lh;
                    }

                frame.AddRange(sprites);
            }
        }


        private void AggregateAllContainersInLogicalGroup(IMyCubeGrid rootGrid, List<Entry> details)
        {
            if (rootGrid == null) return;

            var grids = new List<IMyCubeGrid>();
            try
            {
                MyAPIGateway.GridGroups.GetGroup(rootGrid, GridLinkTypeEnum.Logical, grids);
            }
            catch
            {
            }

            var hasRoot = false;
            for (var i = 0; i < grids.Count; i++)
                if (grids[i] == rootGrid)
                {
                    hasRoot = true;
                    break;
                }

            if (!hasRoot) grids.Insert(0, rootGrid);

            var slims = new List<IMySlimBlock>();
            for (var gi = 0; gi < grids.Count; gi++)
            {
                var g = grids[gi];
                if (g == null) continue;

                slims.Clear();
                g.GetBlocks(slims);

                for (var i = 0; i < slims.Count; i++)
                {
                    var fat = slims[i].FatBlock as IMyTerminalBlock;
                    if (fat == null) continue;

                    var typeIdStr = "";
                    try
                    {
                        typeIdStr = fat.BlockDefinition.TypeIdString ?? fat.BlockDefinition.TypeId.ToString();
                    }
                    catch
                    {
                    }

                    if (typeIdStr.IndexOf("CargoContainer", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    if (!fat.HasInventory) continue;

                    double localUsed = 0, localCap = 0;
                    var invCount = 0;
                    try
                    {
                        invCount = fat.InventoryCount;
                    }
                    catch
                    {
                    }

                    for (var k = 0; k < invCount; k++)
                    {
                        var inv = fat.GetInventory(k);
                        if (inv == null) continue;
                        try
                        {
                            localUsed += (double)inv.CurrentVolume;
                            localCap += (double)inv.MaxVolume;
                        }
                        catch
                        {
                        }
                    }

                    if (localCap > 0)
                    {
                        string name;
                        try
                        {
                            name = fat.CustomName;
                            if (string.IsNullOrEmpty(name)) name = fat.DisplayNameText;
                            if (string.IsNullOrEmpty(name)) name = fat.BlockDefinition.SubtypeName;
                            if (string.IsNullOrEmpty(name)) name = "Container";
                        }
                        catch
                        {
                            name = "Container";
                        }

                        details.Add(new Entry { Name = name, Used = localUsed, Cap = localCap });
                    }
                }
            }
        }


        private class Entry
        {
            public double Cap;
            public string Name;
            public double Used;
        }
    }
}