using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Graph.Extensions;
using Graph.Helpers;
using Graph.Panels;
using Graph.System;
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

        public ContainerGraph(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface.ContentType = ContentType.SCRIPT;
        }

        protected override string DefaultTitle => TITLE;
        public override Dictionary<MyItemType, double> ItemSource => null;

        Color _scriptForegroundColor;
        readonly List<BarPanel> _barPanels = new List<BarPanel>(8);
        int _barPanelCursor;
        
        public override void Run()
        {
            base.Run();
            if (Config == null) return;

            if(_scriptForegroundColor != Surface.ScriptForegroundColor)
                LayoutChanged();
            
            Scale = GetAutoScaleUniform();
            UpdateViewBox();

            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();
                _barPanelCursor = 0;

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
                {
                    switch (Config.DisplayMode)
                    {
                        case DisplayMode.Grid:
                            DrawGrid(sprites, details);
                            break;
                        default:
                            DrawList(sprites, details);
                            break;
                    }
                }

                frame.AddRange(sprites);
            }
        }

        protected override void LayoutChanged()
        {
            _scriptForegroundColor = Surface.ScriptForegroundColor;
            _barPanels.Clear();
            base.LayoutChanged();
        }

        protected void DrawRow(List<MySprite> frame, Entry item, bool showScrollBar)
        {
            var margin = ViewBox.Size.X * Margin;
            Vector2 position = ViewBox.Position;
            position.X += margin;
            position.Y = CaretY;

            var pct = MathHelper.Clamp(item.Used / item.Cap, 0, 1);

            if (Config.DrawLines)
            {
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = new Vector2(ViewBox.Center.X, position.Y),
                    Size = new Vector2(ViewBox.Width - 2f * margin, 2f),
                    Color = ForegroundColor,
                    Alignment = TextAlignment.CENTER
                });
            }

            var clip = new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + (ViewBox.X) - 145 * Scale),
                (int)(LINE_HEIGHT * Scale));

            var barMargin = 8 * Scale;
            Vector2 size;
            if (showScrollBar)
                size = new Vector2(ViewBox.Width - position.X + (ViewBox.X) - SCROLLER_WIDTH * Scale, clip.Height) -
                       barMargin;
            else
                size = new Vector2(ViewBox.Width - position.X + (ViewBox.X), clip.Height) - barMargin;

            var barPanel = GetNextBarPanel(
                new Vector2(clip.Location.X, clip.Location.Y + 1 * Scale) + barMargin / 2f,
                size,
                Config.HeaderColor,
                BackgroundColor.DeriveAscentColor());
            frame.AddRange(barPanel.GetSprites((float)pct, GetContainerUsageColor((float)pct)));

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
            
            frame.Add(text);

            frame.Add(MySprite.CreateClearClipRect());

            position.X = ViewBox.Width + ViewBox.X - margin;
            if (showScrollBar)
                position.X -= SCROLLER_WIDTH * Scale;

            var percentage = new MySprite
            {
                Type = SpriteType.TEXT,
                Data = pct.ToString("P"),
                Position = position,
                RotationOrScale = Scale,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            };

            frame.Add(percentage);

            CaretY += LINE_HEIGHT * Scale;
        }

        void DrawList(List<MySprite> sprites, List<Entry> entries)
        {
            var rowHeight = LINE_HEIGHT * Scale;
            var viewportAvailableHeight = ViewBox.Height - (CaretY - ViewBox.Y) - FooterHeight;
            int maxRows = Math.Max(1, (int)Math.Floor(viewportAvailableHeight / rowHeight));
            bool shouldScroll = entries.Count > maxRows;

            int start = 0;
            if (shouldScroll)
            {
                int totalSteps = Math.Max(1, entries.Count - maxRows);
                int step = GetScrollStep(SCROLL_DELAY / 6);
                start = step % (totalSteps + 1);

                float viewportHeight = maxRows * rowHeight - (SCROLLER_WIDTH * 2 * Scale);
                float scrollBarHeight = (float)maxRows / entries.Count * viewportHeight;
                float totalScrollableRows = entries.Count - maxRows;
                float scrollFraction = totalScrollableRows > 0 ? start / totalScrollableRows : 0f;
                float scrollBarTravel = viewportHeight - scrollBarHeight;
                float scrollBarY = scrollFraction * scrollBarTravel;
                float scrollBarCenter = scrollBarY + scrollBarHeight / 2f;
                float initialY = CaretY + SCROLLER_WIDTH * Scale;

                DrawScrollBar(sprites, Scale, initialY, viewportHeight, scrollBarCenter, scrollBarHeight);
            }

            int showCount = Math.Min(maxRows, entries.Count - start);
            for (int i = 0; i < showCount; i++)
            {
                int idx = start + i;
                DrawRow(sprites, entries[idx], shouldScroll);
            }
        }

        void DrawGrid(List<MySprite> sprites, List<Entry> entries)
        {
            var rowHeight = 2f * LINE_HEIGHT * Scale;
            var viewportAvailableHeight = ViewBox.Height - (CaretY - ViewBox.Y) - FooterHeight;
            int maxRows = Math.Max(1, (int)Math.Floor(viewportAvailableHeight / rowHeight));
            int maxCols = Math.Max(1, GetMaxColsFromSurface());

            int maxVisible = maxRows * maxCols;
            bool shouldScroll = entries.Count > maxVisible;
            int startRow = 0;

            if (shouldScroll)
            {
                int totalRows = (int)Math.Ceiling(entries.Count / (float)maxCols);
                int totalSteps = Math.Max(1, totalRows - maxRows);
                int step = GetScrollStep(SCROLL_DELAY / 6);
                startRow = step % (totalSteps + 1);

                float viewportHeight = maxRows * rowHeight - (SCROLLER_WIDTH * 2 * Scale);
                float scrollBarHeight = (float)maxRows / totalRows * viewportHeight;
                float totalScrollableRows = totalRows - maxRows;
                float scrollFraction = totalScrollableRows > 0 ? startRow / totalScrollableRows : 0f;
                float scrollBarTravel = viewportHeight - scrollBarHeight;
                float scrollBarY = scrollFraction * scrollBarTravel;
                float scrollBarCenter = scrollBarY + scrollBarHeight / 2f;
                float initialY = CaretY + SCROLLER_WIDTH * Scale;

                DrawScrollBar(sprites, Scale, initialY, viewportHeight, scrollBarCenter, scrollBarHeight);
            }

            int start = startRow * maxCols;
            int showCount = Math.Min(maxVisible, entries.Count - start);

            float margin = ViewBox.Width * Margin;
            float contentStart = ViewBox.X + margin;
            float contentEnd = ViewBox.Width + ViewBox.X - margin;
            if (shouldScroll)
                contentEnd -= SCROLLER_WIDTH * Scale;
            float columnWidth = (contentEnd - contentStart) / maxCols;
            float gridHeight = maxRows * rowHeight;

            if (Config.DrawLines)
            {
                var lineColor = Config.HeaderColor;
                for (int row = 0; row <= maxRows; row++)
                {
                    var y = CaretY + row * rowHeight;
                    sprites.Add(new MySprite
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = new Vector2((contentStart + contentEnd) / 2f, y),
                        Size = new Vector2(contentEnd - contentStart, 2f),
                        Color = lineColor,
                        Alignment = TextAlignment.CENTER
                    });
                }

                for (int col = 0; col <= maxCols; col++)
                {
                    var x = contentStart + col * columnWidth;
                    sprites.Add(new MySprite
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = new Vector2(x, CaretY + gridHeight / 2f),
                        Size = new Vector2(2f, gridHeight),
                        Color = lineColor,
                        Alignment = TextAlignment.CENTER
                    });
                }
            }

            for (int gridIdx = 0; gridIdx < showCount; gridIdx++)
            {
                int idx = start + gridIdx;
                int col = gridIdx % maxCols;
                int row = gridIdx / maxCols;
                float xStart = contentStart + col * columnWidth;
                float xEnd = (col == maxCols - 1) ? contentEnd : xStart + columnWidth;
                float yStart = CaretY + row * rowHeight;
                DrawGridCell(sprites, entries[idx], xStart, xEnd, yStart, rowHeight);
            }
        }

        void DrawGridCell(List<MySprite> frame, Entry item, float xStart, float xEnd, float yStart, float rowHeight)
        {
            var cellPadding = (LINE_HEIGHT * Scale) / 3f;
            var pct = MathHelper.Clamp(item.Cap > 0 ? item.Used / item.Cap : 0, 0, 1);
            var cellView = GetCellViewBox(xStart, xEnd, yStart, rowHeight, cellPadding);

            if (!Config.DrawLines)
            {
                var backgroundColor = Config.HeaderColor;
                var hsv = backgroundColor.ColorToHSV();
                hsv.Z *= 0.2f;
                var cellRect = new RectangleF(
                    xStart + cellPadding / 2f,
                    yStart + cellPadding / 2f,
                    (xEnd - xStart) - cellPadding,
                    rowHeight - cellPadding);
                var dropShadow = new RectangleF(cellRect.Position + 2, cellRect.Size);
                RectanglePanel.CreateSpritesFromRect(dropShadow, frame, hsv.HSVtoColor(), .2f);
                RectanglePanel.CreateSpritesFromRect(cellRect, frame, backgroundColor, .2f);
            }

            var nameHeight = Math.Max(0f, cellView.Height * .45f);
            var nameRect = new RectangleF(cellView.X, cellView.Y, cellView.Width, nameHeight);
            var bottomRect = new RectangleF(cellView.X, nameRect.Bottom, cellView.Width, Math.Max(0f, cellView.Bottom - nameRect.Bottom));

            var name = new StringBuilder(item.Name ?? string.Empty);
            TrimText(ref name, nameRect.Width);

            var namePos = new Vector2(nameRect.X + 2f * Scale, nameRect.Y + 2f * Scale);
            frame.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = name.ToString(),
                Position = namePos,
                RotationOrScale = .9f * Scale,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            var barWidth = bottomRect.Width * (2f / 3f);
            var textRect = new RectangleF(bottomRect.X + barWidth, bottomRect.Y, bottomRect.Width - barWidth, bottomRect.Height);
            var barRect = new RectangleF(bottomRect.X, bottomRect.Y, barWidth, bottomRect.Height);

            var barInnerPaddingX = 2f * Scale;
            var barInnerPaddingY = bottomRect.Height * 0.2f;
            var barPanel = GetNextBarPanel(
                new Vector2(barRect.X + barInnerPaddingX, barRect.Y + barInnerPaddingY + (2f * Scale)),
                new Vector2(
                    Math.Max(1f, barRect.Width - 2f * barInnerPaddingX),
                    Math.Max(1f, barRect.Height - 2f * barInnerPaddingY)),
                Config.HeaderColor.DeriveAscentColor(),
                BackgroundColor.DeriveAscentColor());
            frame.AddRange(barPanel.GetSprites((float)pct, GetContainerUsageColor((float)pct)));

            var pctText = ((float)pct).ToString("P0", CultureInfo.CurrentUICulture);
            var pctPos = new Vector2(textRect.Right - (2f * Scale), textRect.Y + 2f * Scale);
            frame.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = pctText,
                Position = pctPos,
                RotationOrScale = .95f * Scale,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });
        }

        int GetMaxColsFromSurface()
        {
            var max = ViewBox.Width - ViewBox.X;
            var perCol = MINIMUM_COL_WIDTH * Scale;
            return (int)Math.Max(1, Math.Round(max / perCol - .5, MidpointRounding.AwayFromZero));
        }

        BarPanel GetNextBarPanel(Vector2 posTopLeft, Vector2 size, Color fillColor, Color bgColor)
        {
            if (_barPanelCursor >= _barPanels.Count)
                _barPanels.Add(BuildBarPanel(posTopLeft, size, fillColor, bgColor));

            return _barPanels[_barPanelCursor++];
        }

        static BarPanel BuildBarPanel(Vector2 posTopLeft, Vector2 size, Color fillColor, Color bgColor)
        {
            return new BarPanel(posTopLeft, size, fillColor, bgColor);
        }

        Color? GetContainerUsageColor(float pct)
        {
            if (pct >= .99f)
                return Config.ErrorColor;
            if (pct > .90f)
                return Config.WarningColor;
            return null;
        }

        void DrawScrollBar(List<MySprite> frame, float scale, float initialY, float viewportHeight,
            float scrollBarCenter, float scrollBarHeight)
        {
            float barXCenter = ViewBox.X + ViewBox.Width - (SCROLLER_WIDTH / 2f) * scale;
            int barWidth = (int)(SCROLLER_WIDTH * scale);

            var trackCenter = new Vector2(barXCenter,
                (float)Math.Round(initialY + viewportHeight / 2f, MidpointRounding.ToEven));
            DrawCapsule(frame, trackCenter, barWidth, viewportHeight,
                new Color(Surface.ScriptForegroundColor.R, Surface.ScriptForegroundColor.G,
                    Surface.ScriptForegroundColor.B, 127));

            var thumbCenter = new Vector2(barXCenter,
                (float)Math.Round(initialY + scrollBarCenter, MidpointRounding.ToEven));
            DrawCapsule(frame, thumbCenter, barWidth, scrollBarHeight,
                new Color(Config.HeaderColor.R, Config.HeaderColor.G, Config.HeaderColor.B, 250));
        }

        void DrawCapsule(List<MySprite> frame, Vector2 center, int width, float height, Color color)
        {
            frame.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareSimple",
                Position = center,
                Size = new Vector2(width, height + .5f),
                Color = color,
                Alignment = TextAlignment.CENTER
            });

            var capsSize = new Vector2(width);

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SemiCircle",
                Position = new Vector2(center.X, center.Y - height / 2f),
                Size = capsSize,
                RotationOrScale = 0f,
                Color = color,
                Alignment = TextAlignment.CENTER
            });

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SemiCircle",
                Position = new Vector2(center.X, center.Y + height / 2f),
                Size = capsSize,
                RotationOrScale = (float)Math.PI,
                Color = color,
                Alignment = TextAlignment.CENTER
            });
        }

        const int SCROLLER_WIDTH = 8;
        const int LINE_HEIGHT = 40;
        const int MINIMUM_COL_WIDTH = 220;
        const int SCROLL_DELAY = 12;

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
