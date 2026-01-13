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

        string Req = "Req";
        string Mis = "Mis";

        readonly Vector2 _piePosition = new Vector2(10 + PIE_RADIUS / 2, -5);
        const float PIE_RADIUS = 40;
        readonly PieDualChartPanel _pieBlueprint;

        public ProjectorCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size)
            : base(surface, block, size)
        {
            _pieBlueprint = new PieDualChartPanel(
                "",
                (IMyTextSurface)Surface,
                ToScreenMargin(new Vector2(ViewBox.Position.X, ViewBox.Bottom) + _piePosition * Scale),
                new Vector2(PIE_RADIUS * Scale),
                false
            );
        }

        protected override void LayoutChanged()
        {
            base.LayoutChanged();
            _pieBlueprint.SetMargin(
                ToScreenMargin(new Vector2(ViewBox.Position.X, ViewBox.Bottom) + _piePosition * Scale),
                new Vector2(PIE_RADIUS * Scale));

            _customTitle = _projector?.CustomName;

            Req = MyTexts.Get(MyStringId.GetOrCompute("ScreenTerminalProduction_RequiredAndAvailable")).ToString().Substring(0, 3);
            Mis = MyTexts.Get(MyStringId.GetOrCompute("AssemblerState_MissingItems")).ToString().Substring(0, 3);
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

            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = Icon,
                Position = position + new Vector2(10f, 20) * Scale,
                Size = new Vector2(40 * Scale),
                Color = Config.HeaderColor,
                Alignment = TextAlignment.CENTER
            });
            position.X += ViewBox.Width / 8f;

            var numberWidth = NUMBER_WIDTH * Scale;

            var availableSize = new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + (ViewBox.X) - (2 * margin) - (2 * numberWidth)),
                (int)(position.Y + TITLE_HEIGHT * Scale));
            frame.Add(MySprite.CreateClipRect(availableSize));


            StringBuilder displayNameSb = new StringBuilder(Title);
            string displayName;

            if (!TitleCache.TryGetValue(Title + Scale, out displayName))
            {
                TitleCache.Clear();

                StringBuilder trimmedSb = new StringBuilder(displayNameSb.ToString());

                TrimText(ref trimmedSb, availableSize.Width, 1.3f);

                displayName = trimmedSb.ToString();
                TitleCache[displayNameSb.ToString() + Scale] = displayName;
            }

            frame.Add(new MySprite()
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
            position.X = ViewBox.Width + ViewBox.X - margin - SCROLLER_WIDTH * Scale;
            frame.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = Mis,
                Position = position,
                RotationOrScale = Scale * 1.3f,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            position.X -= numberWidth;

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = Req,
                Position = position,
                RotationOrScale = Scale * 1.3f,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            CaretY += 40 * Scale;
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

                SpriteCache[item.Key] = sprite;
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

            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = sprite,
                Position = position + new Vector2(10f, 15) * Scale,
                Size = new Vector2(LINE_HEIGHT * Scale),
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.CENTER
            });
            position.X += ViewBox.Width / 8f;

            var clip = new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + (ViewBox.X) - 105 * Scale),
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
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });
            frame.Add(MySprite.CreateClearClipRect());
            position.X = ViewBox.Width + ViewBox.X - margin;
            if (showScrollBar) position.X -= SCROLLER_WIDTH * Scale;
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = FormatItemQty(item.Value),
                Position = position,
                RotationOrScale = Scale,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            position.X -= NUMBER_WIDTH * Scale;

            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = FormatItemQty(_needed[item.Key]),
                Position = position,
                RotationOrScale = Scale,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            CaretY += LINE_HEIGHT * Scale;
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