using System;
using System.Collections.Generic;
using System.Globalization;
using Graph.Extensions;
using Graph.Helpers;
using Graph.Panels;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using IMyCubeBlock = VRage.Game.ModAPI.IMyCubeBlock;
using IMyCubeGrid = VRage.Game.ModAPI.IMyCubeGrid;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;

namespace Graph.Charts
{
    [MyTextSurfaceScript(ID, TITLE)]
    public class ContainerGraph : ChartBase
    {
        public const string ID = "ContainerCharts";
        public const string TITLE = "DisplayName_CargoFilledEntityComponent";

        List<BarPanel> barCache = new List<BarPanel>();

        public ContainerGraph(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface.ContentType = ContentType.SCRIPT;
        }

        protected override string DefaultTitle => TITLE;
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
                GetContainers((IMyCubeGrid)Block?.CubeGrid, details);

                details.Sort((a, b) =>
                {
                    var fa = a.Cap > 0 ? a.Used / a.Cap : 0;
                    var fb = b.Cap > 0 ? b.Used / b.Cap : 0;
                    var cmp = fb.CompareTo(fa);
                    if (cmp != 0) return cmp;
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });


                if (details.Count == 0)
                    sprites.Add(new MySprite
                    {
                        Type = SpriteType.TEXT,
                        Data = LocHelper.Empty,
                        Position = new Vector2(ViewBox.Position.X + 12f * Scale, CaretY),
                        Color = Surface.ScriptForegroundColor,
                        Alignment = TextAlignment.LEFT,
                        RotationOrScale = 0.88f * Scale
                    });
                else
                    for (var index = 0; index < details.Count; index++)
                    {
                        var t = details[index];
                        BarPanel panel = null;

                        if (barCache.Count > index)
                            panel = barCache[index];

                        DrawRow(sprites, t, true, ref panel);

                        if (barCache.Count <= index)
                            barCache.Add(panel);
                    }

                frame.AddRange(sprites);
            }
        }

        protected override void LayoutChanged()
        {
            base.LayoutChanged();
            barCache.Clear();
        }

        protected void DrawRow(List<MySprite> frame, Entry item, bool showScrollBar, ref BarPanel barPanel)
        {
            var margin = ViewBox.Size.X * Margin;
            Vector2 position = ViewBox.Position;
            position.X += margin;
            position.Y = CaretY;

            var pct = MathHelper.Clamp(item.Used / item.Cap, 0, 1);

            var clip = new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + (ViewBox.X) - 145 * Scale),
                (int)(LINE_HEIGHT * Scale));

            var barMargin = 8 * Scale;

            if (barPanel == null)
            {
                Vector2 size;
                if (showScrollBar)
                    size = new Vector2(ViewBox.Width - position.X + (ViewBox.X) - SCROLLER_WIDTH * Scale, clip.Height) -
                           barMargin;
                else
                    size = new Vector2(ViewBox.Width - position.X + (ViewBox.X), clip.Height) - barMargin;

                barPanel = new BarPanel(new Vector2(clip.Location.X, clip.Location.Y) + barMargin / 2, size
                    , Config.HeaderColor, Surface.ScriptForegroundColor);
            }

            frame.AddRange(barPanel.GetSprites((float)pct));

            frame.Add(MySprite.CreateClipRect(clip));

            position.X += 16 * Scale;
            position.Y += 4 * Scale;
            
            var text = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = item.Name,
                Position = position,
                RotationOrScale = Scale,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            };

            var shadowOffset = 2 * Scale;
            frame.Add(text.Shadow(2*Scale));
            frame.Add(text);

            frame.Add(MySprite.CreateClearClipRect());

            position.X = ViewBox.Width + ViewBox.X - margin;
            if (showScrollBar)
                position.X -= SCROLLER_WIDTH * Scale;

            var percentage = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = pct.ToString("P"),
                Position = position,
                RotationOrScale = Scale,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            };
            frame.Add(percentage.Shadow(shadowOffset));
            frame.Add(percentage);

            CaretY += LINE_HEIGHT * Scale;
        }

        const int SCROLLER_WIDTH = 8;
        const int LINE_HEIGHT = 40;

        void GetContainers(IMyCubeGrid rootGrid, List<Entry> details)
        {
            AggregateAllContainersInLogicalGroup(rootGrid, details);
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


        public class Entry
        {
            public double Cap;
            public string Name;
            public double Used;
        }
    }
}