using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Graph.Helpers;
using Graph.Panels;
using Graph.System;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Graph.Charts
{
    [MyTextSurfaceScript(ID, "DisplayName_Block_Projector")]
    public class ProjectorCharts : ItemCharts
    {
        public const string ID = "ProjectorCharts";
        public const string TITLE = "DisplayName_Block_Projector";

        protected const int NUMBER_WIDTH = 104;

        public string[] AllowedTypes = { "Component" };

        protected override string DefaultTitle => _customTitle ?? TITLE;

        string _customTitle;

        IMyProjector _projector;

        public override Dictionary<MyItemType, double> ItemSource => _missing;

        readonly Dictionary<MyItemType, double> _missing = new Dictionary<MyItemType, double>();
        readonly Dictionary<MyItemType, int> _needed = new Dictionary<MyItemType, int>();

        int _totalBlocks = 1;
        int _remainingBlocks;

        int _totalComponents;
        int _missingComponents;
        
        string Required = "Req";
        string Available = "Ava";

        float RequiredX;
        float AvailableX;
        
        const float PIE_RADIUS = 40;
        readonly PieDualChartPanel _pieBlueprint;

        public ProjectorCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size)
            : base(surface, block, size)
        {
            _pieBlueprint = new PieDualChartPanel(
                "",
                (IMyTextSurface)Surface,
                ToScreenMargin(GetFooterPieCenter()),
                new Vector2(PIE_RADIUS * Scale),
                false
            );
        }

        protected override void LayoutChanged()
        {
            base.LayoutChanged();
            _pieBlueprint.SetMargin(
                ToScreenMargin(GetFooterPieCenter()),
                new Vector2(PIE_RADIUS * Scale));

            _customTitle = _projector?.CustomName;

            var RaA = MyTexts.Get(MyStringId.GetOrCompute("ScreenTerminalProduction_RequiredAndAvailable")).ToString().Split('/');
            if (RaA.Length == 2)
            {
                Required = RaA.First().Trim();
                Available = RaA.Last().Trim();
            }
            
            RequiredX = Surface.MeasureStringInPixels(new StringBuilder(Required), "White", 1).X;
            AvailableX = Surface.MeasureStringInPixels(new StringBuilder(Available), "White", 1).X;
        }

        protected override void DrawTitle(List<MySprite> frame)
        {
            var margin = ViewBox.Size.X * Margin;

            Vector2 position = ViewBox.Position;
            position.X += margin;
            position.Y += (ViewBox.Size.Y * Margin) / 2;

            CaretY = position.Y;

            if (!TitleVisible)
                return;

            AddHeaderSprite(frame, new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = Icon,
                Position = position + new Vector2(20) * Scale,
                Size = new Vector2(40 * Scale),
                Color = Config.HeaderColor,
                Alignment = TextAlignment.CENTER
            });
            position.X += ViewBox.Width / 8f;

            var numberWidth = GetQuantityColumnWidth();
            var headerSeparatorPadding = 10f * Scale;

            var availableSize = new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + (ViewBox.X) - (2 * margin) - (2 * numberWidth) - (2 * headerSeparatorPadding)),
                (int)(position.Y + TITLE_HEIGHT * Scale));
            frame.Add(MySprite.CreateClipRect(availableSize));


            var displayName = GetCachedTitleText(availableSize.Width, 1.3f, false);

            AddHeaderSprite(frame, new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = displayName,
                Position = position,
                RotationOrScale = Scale * 1.3f,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            frame.Add(MySprite.CreateClearClipRect());
            var requiredRight = ViewBox.Width + ViewBox.X - margin - SCROLLER_WIDTH * Scale;
            var availableRight = requiredRight - numberWidth - (2f * headerSeparatorPadding);
            var separatorX = requiredRight - numberWidth - headerSeparatorPadding;

            position.X = requiredRight;
            AddHeaderSprite(frame, new MySprite
            {
                Type = SpriteType.TEXT,
                Data = Required,
                Position = position,
                RotationOrScale = Scale * 1.3f,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            AddHeaderSprite(frame, new MySprite
            {
                Type = SpriteType.TEXT,
                Data = "/",
                Position = new Vector2(separatorX, position.Y),
                RotationOrScale = Scale * 1.3f,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            position.X = availableRight;
            AddHeaderSprite(frame, new MySprite
            {
                Type = SpriteType.TEXT,
                Data = Available,
                Position = position,
                RotationOrScale = Scale * 1.3f,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            CaretY += TitleBarHeightBase * Scale;
        }

        protected override void DrawFooter(List<MySprite> frame)
        {
            EnsureData();

            if (_projector?.CustomName != _customTitle)
                LayoutChanged();

            if (_projector == null)
                return;

            if (_totalBlocks == 0 || _totalComponents == 0)
                return;

            var margin = ViewBox.Size.X * 0.02f;
            var pos = ViewBox.Position;
            pos.X += margin;

            int built = Math.Max(_totalBlocks - _remainingBlocks, 0);

            FooterHeight = 25f * 2 * Scale;
            pos.X += 25f * 2 * Scale;

            pos.Y = ViewBox.Bottom - FooterHeight;

            var legendSize = new Vector2(8, 8) * Scale;

            var blocksString = MyTexts.GetString("TerminalTab_Info_Blocks");

            pos.X += legendSize.X;

            var lineSpacer = 25f * Scale;

            var blocksPct = built / (float)_totalBlocks;
            var componentsPct = 1 - (float)_missingComponents / _totalComponents;

            StringBuilder sb = new StringBuilder($"{blocksString}{blocksPct:P2}  ({built}/{_totalBlocks} )");

            TrimText(ref sb, ViewBox.Width - pos.X - ViewBox.X, 0.9f);

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = sb.ToString(),
                Position = pos,
                RotationOrScale = Scale * 0.9f,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            pos.Y += lineSpacer;

            var components = MyTexts.GetString("DisplayName_InventoryConstraint_Components");

            sb.Clear();
            sb.Append(
                $"{components}: {componentsPct:P2}  ({(_totalComponents - _missingComponents).ToString(CultureInfo.CurrentUICulture)}" +
                $"/{_totalComponents.ToString(CultureInfo.CurrentUICulture)})");


            TrimText(ref sb, ViewBox.Width - pos.X - ViewBox.X, 0.9f);

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = sb.ToString(),
                Position = pos,
                RotationOrScale = Scale * 0.9f,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            pos.X -= legendSize.X;

            pos.Y -= lineSpacer - (legendSize.Y + legendSize.Y / 2);

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = pos,
                Size = legendSize,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.CENTER,
            });

            pos.Y += lineSpacer;

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = pos,
                Size = legendSize,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.CENTER,
            });

            frame.AddRange(_pieBlueprint.GetSprites(componentsPct, blocksPct, Config.HeaderColor, true));
        }

        protected override List<KeyValuePair<MyItemType, double>> ReadItems(IMyTerminalBlock lcd)
        {
            if (lcd == null || ItemSource == null)
                return new List<KeyValuePair<MyItemType, double>>();

            var list = ItemSource.ToList();
            switch (SortMethod)
            {
                case SortMethod.Type:
                    list.Sort((a, b) =>
                    {
                        var typeCmp = string.Compare(a.Key.TypeId, b.Key.TypeId, StringComparison.CurrentCulture);
                        if (typeCmp != 0)
                            return typeCmp;
                        return string.Compare(a.Key.SubtypeId, b.Key.SubtypeId, StringComparison.CurrentCulture);
                    });
                    break;
                default:
                    list.Sort((a, b) => b.Value.CompareTo(a.Value));
                    break;
            }

            return list;
        }

        protected override void DrawRow(List<MySprite> frame, KeyValuePair<MyItemType, double> item, bool showScrollBar)
        {
            string sprite;
            string localizedName;

            if (!SpriteCache.TryGetValue(item.Key, out sprite))
            {
                var reference = new List<string>();
                var color = "ColorfulIcons_" + item.Key.ToString().Substring(16);
                const string NOT_FOUND = "Textures\\FactionLogo\\Unknown.dds";

                Surface.GetSprites(reference);
                if (reference.Contains(color))
                    sprite = color;
                else if (reference.Contains(item.Key.ToString()))
                    sprite = item.Key.ToString();
                else sprite = NOT_FOUND;

                AddToSpriteCache(item.Key, sprite);
            }

            var margin = ViewBox.Size.X * Margin;
            Vector2 position = ViewBox.Position;
            position.X += margin;
            position.Y = CaretY;

            bool drawSeparatorLine = Config.SortMethod == SortMethod.Type && _previousType != item.Key.TypeId;

            if (Config.DrawLines || drawSeparatorLine)
            {
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Circle",
                    Position = new Vector2(ViewBox.Center.X, position.Y),
                    Size = new Vector2(ViewBox.Width - 2 * margin, 1),
                    Color = drawSeparatorLine ? Config.HeaderColor : Surface.ScriptForegroundColor,
                    Alignment = TextAlignment.CENTER
                });
            }

            _previousType = item.Key.TypeId;
            var hasShortage = HasShortage(item.Key, item.Value);

            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = sprite,
                Position = position + new Vector2(20f, 15) * Scale,
                Size = new Vector2(LINE_HEIGHT * Scale),
                Color = hasShortage ? new Color(96, 32, 32) : Surface.ScriptForegroundColor,
                Alignment = TextAlignment.CENTER
            });
            position.X += ViewBox.Width / 8f;
            var quantityColumnsWidth = 2f * GetQuantityColumnWidth() + GetQuantityColumnGap();

            var clip = new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + (ViewBox.X) - quantityColumnsWidth - margin),
                (int)(position.Y + (LINE_HEIGHT + 5) * Scale));

            frame.Add(MySprite.CreateClipRect(clip));

            if (!_locKeysCache.TryGetValue(item.Key, out localizedName))
            {
                var key =
                    MyDefinitionManager.Static.TryGetPhysicalItemDefinition(item.Key).DisplayNameEnum?.ToString() ??
                    item.Key.SubtypeId;
                var sb = new StringBuilder(MyTexts.GetString(key));
                TrimText(ref sb, clip.Width);
                localizedName = sb.ToString();
                _locKeysCache[item.Key] = sb.ToString();
            }

            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = localizedName,
                Position = position,
                RotationOrScale = Scale,
                Color = hasShortage ? new Color(96, 32, 32) : Surface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });
            frame.Add(MySprite.CreateClearClipRect());
            position.X = ViewBox.Width + ViewBox.X - margin;
            if (showScrollBar) position.X -= SCROLLER_WIDTH * Scale;
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = FormatItemQty(GetNeededQty(item.Key)),
                Position = position,
                RotationOrScale = Scale,
                Color = hasShortage ? new Color(96, 32, 32) : Surface.ScriptForegroundColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });
            position.X -= GetQuantityColumnWidth() + GetQuantityColumnGap();
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = FormatItemQty(GetAvailableQty(item.Key, item.Value)),
                Position = position,
                RotationOrScale = Scale,
                Color = hasShortage ? new Color(96, 32, 32) : Surface.ScriptForegroundColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            CaretY += LINE_HEIGHT * Scale;
        }

        protected override void DrawCellContent(List<MySprite> frame, KeyValuePair<MyItemType, double> item,
            string sprite, Color foreground, MyTuple<RectangleF, RectangleF, RectangleF> slots)
        {
            string localizedName;
            var iconRect = slots.Item1;
            var numberRect = slots.Item2;
            var nameRect = slots.Item3;
            var hasShortage = HasShortage(item.Key, item.Value);
            var useAlertText = hasShortage && Config.DrawLines;
            var color = useAlertText ? new Color(96, 32, 32) : foreground;

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = sprite,
                Position = new Vector2(iconRect.X, iconRect.Y + iconRect.Height / 2f),
                Size = new Vector2(iconRect.Width),
                Alignment = TextAlignment.LEFT,
                Color = useAlertText ? new Color(96, 32, 32) : Color.White
            });

            if (!_locKeysCache.TryGetValue(item.Key, out localizedName))
            {
                var key =
                    MyDefinitionManager.Static.TryGetPhysicalItemDefinition(item.Key).DisplayNameEnum?.ToString() ??
                    item.Key.SubtypeId;
                var sb = new StringBuilder(MyTexts.GetString(key));
                TrimText(ref sb, nameRect.Width);
                localizedName = sb.ToString();
                _locKeysCache[item.Key] = sb.ToString();
            }

            Vector2 size = GetSizeInPixel(localizedName, "White", 1, Surface);
            float minProportion = Math.Min(nameRect.Width / size.X, nameRect.Height / size.Y);
            float fontSize = minProportion;
            float renderedHeight = size.Y * fontSize;
            Vector2 pos = nameRect.Center;
            pos.Y -= renderedHeight * 0.5f;
            pos.X = nameRect.Right;

            frame.Add(new MySprite(
                SpriteType.TEXT,
                localizedName,
                pos,
                null,
                color,
                "White",
                TextAlignment.RIGHT,
                fontSize * .95f
            ));

            var qty = FormatItemQty(GetAvailableQty(item.Key, item.Value)) + "/" + FormatItemQty(GetNeededQty(item.Key));
            size = GetSizeInPixel(qty, "White", 1, Surface);
            minProportion = Math.Min(numberRect.Width / size.X, numberRect.Height / size.Y);
            fontSize = minProportion;
            renderedHeight = size.Y * fontSize;
            pos = numberRect.Center;
            pos.Y -= renderedHeight * 0.5f;
            pos.X = numberRect.Right;

            frame.Add(new MySprite(
                SpriteType.TEXT,
                qty,
                pos,
                null,
                color,
                "White",
                TextAlignment.RIGHT,
                fontSize * .95f
            ));
        }

        protected override void DrawCellBackground(List<MySprite> frame, KeyValuePair<MyItemType, double> item,
            float xStart, float xEnd, float yStart, float cellHeight, float cellPadding)
        {
            var rl = xStart + cellPadding / 2;
            var rr = xEnd - cellPadding / 2;
            var rt = yStart + cellPadding / 2;
            var rb = yStart + cellHeight - cellPadding / 2;

            var backgroundColor = HasShortage(item.Key, item.Value) ? new Color(96, 32, 32) : Config.HeaderColor;
            var a = backgroundColor.ColorToHSV();
            a.Z *= 0.2f;
            var cellRect = new RectangleF(rl, rt, rr - rl, rb - rt);
            var dropShadow = new RectangleF(cellRect.Position + 2, cellRect.Size);
            RectanglePanel.CreateSpritesFromRect(dropShadow, frame, a.HSVtoColor(), .2f);
            RectanglePanel.CreateSpritesFromRect(cellRect, frame, backgroundColor, .2f);
        }

        int GetNeededQty(MyItemType itemType)
        {
            int needed;
            return _needed.TryGetValue(itemType, out needed) ? needed : 0;
        }

        double GetAvailableQty(MyItemType itemType, double missingQty)
        {
            var needed = GetNeededQty(itemType);
            var have = needed - missingQty;
            return have < 0 ? 0 : have;
        }

        bool HasShortage(MyItemType itemType, double missingQty)
        {
            return GetAvailableQty(itemType, missingQty) < GetNeededQty(itemType);
        }

        float GetQuantityColumnWidth()
        {
            var labelWidth = Math.Max(RequiredX, AvailableX) * Scale * 1.3f + (8f * Scale);
            return Math.Max(100f * Scale, labelWidth);
        }

        float GetQuantityColumnGap()
        {
            return 20f * Scale;
        }

        Vector2 GetFooterPieCenter()
        {
            var margin = ViewBox.Size.X * Margin;
            var headerIconCenterX = ViewBox.Position.X + margin + 20f * Scale;
            var footerPieCenterY = ViewBox.Bottom + (-5f * Scale);
            return new Vector2(headerIconCenterX, footerPieCenterY);
        }

        void EnsureData()
        {
            _missing.Clear();
            _totalBlocks = 1;
            _remainingBlocks = 0;
            _totalComponents = 0;
            _missingComponents = 0;

            var lcd = Block as IMyTerminalBlock;

            IMyCubeGrid grid = Block?.CubeGrid as IMyCubeGrid;

            if (grid == null)
                return;

            _projector = FindProjector(grid);

            if (_projector == null)
                return;

            try
            {
                _totalBlocks = Math.Max(_projector.TotalBlocks, 1);
                _remainingBlocks = Math.Max(_projector.RemainingBlocks, 0);
            }
            catch
            {
                _totalBlocks = 1;
                _remainingBlocks = 0;
            }

            try
            {
                _needed.Clear();

                foreach (var block in _projector.RemainingBlocksPerType)
                {
                    var def = (MyCubeBlockDefinition)block.Key;

                    foreach (var perType in def.Components)
                    {
                        int qty;
                        _needed.TryGetValue(perType.Definition.Id, out qty);
                        _needed[perType.Definition.Id] = qty + perType.Count * block.Value;
                    }
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, GetType());
            }

            var availableByType = GetAvailableComponents(lcd);

            long totalNeeded = 0;
            long totalMissing = 0;

            foreach (var needed in _needed)
            {
                double available;
                availableByType.TryGetValue(needed.Key, out available);

                double missing = needed.Value - available;
                if (missing < 0) missing = 0;

                _missing[needed.Key] = Math.Max(0, missing);

                totalNeeded += needed.Value;
                totalMissing += (long)Math.Round(missing);
            }

            _totalComponents = (int)Math.Max(0, totalNeeded);
            _missingComponents = (int)Math.Max(0, totalMissing);
        }

        Dictionary<MyItemType, double> GetAvailableComponents(IMyTerminalBlock referenceBlock)
        {
            try
            {
                var hasFilter = Config.SelectedBlocks.Length > 0 || Config.SelectedGroups.Length > 0;
                return hasFilter ? GridLogic.GetItems(Config, referenceBlock, AllowedTypes) : GridLogic.Components;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, GetType());
            }

            return new Dictionary<MyItemType, double>();
        }

        IMyProjector FindProjector(IMyCubeGrid grid)
        {
            if (Config.ReferenceBlock == 0)
                return null;

            var entity = MyAPIGateway.Entities.GetEntityById(Config.ReferenceBlock);

            var projector = entity as IMyProjector;
            if (projector == null)
                return null;

            return projector.CubeGrid.IsInSameLogicalGroupAs(grid) ? projector : null;
        }
    }
}
