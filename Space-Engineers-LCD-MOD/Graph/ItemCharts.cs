using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Helpers;
using VRage;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Graph.Data.Scripts.Graph
{
    public abstract class ItemCharts : ChartBase
    {
        public static Dictionary<MyItemType, string> SpriteCache =
            new Dictionary<MyItemType, string>();

        public static Dictionary<MyItemType, MyStringId> LocKeysCache =
            new Dictionary<MyItemType, MyStringId>();

        public Dictionary<string, string> TitleCache =
            new Dictionary<string, string>();

        const int TITLE_HEIGHT = 35;
        const int LINE_HEIGHT = 30;
        const int SCROLLER_WIDTH = 8;
        const int SCROLL_DELAY = 12; // 12 means 2 seconds delay (10 ticks per operation, 60 ticks per second)

        long _clock;

        protected ItemCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        public override void Run()
        {
            base.Run();

            _clock++;
            if (_clock % SCROLL_DELAY != 0 && !Dirty)
                return; // skip update by {DELAY} ticks

            if (Config == null)
                return;

            try
            {
                DrawItems();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        public void DrawItems()
        {
            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                DrawTitle(sprites);
                DrawFooter(sprites);

                var items = ReadItems(Block as IMyTerminalBlock);

                if (items.Count == 0)
                {
                    var margin = ViewBox.Size.X * Margin;
                    Vector2 position = ViewBox.Position;
                    position.X += margin;
                    position.Y = CaretY;
                    sprites.Add(MakeText((IMyTextSurface)Surface,
                        $"- {MyTexts.GetString("BlockPropertyProperties_WaterLevel_Empty")} -",
                        ViewBox.Center, Scale, TextAlignment.CENTER));
                }
                else
                {
                    int maxRows = GetMaxRowsFromSurface();
                    if (maxRows < 1)
                        maxRows = 1;

                    bool shouldScroll = items.Count > maxRows;

                    int start = 0;

                    if (shouldScroll)
                    {
                        int totalSteps = items.Count - maxRows;
                        if (totalSteps < 1) totalSteps = 1;

                        int step = GetScrollStep(SCROLL_DELAY / 6);

                        start = step % (totalSteps + 1);

                        float viewportHeight = maxRows * (LINE_HEIGHT * Scale) - (SCROLLER_WIDTH * 2 * Scale);
                        float scrollBarHeight = (float)maxRows / items.Count * viewportHeight;

                        float totalScrollableRows = items.Count - maxRows;
                        float scrollFraction = (totalScrollableRows > 0) ? start / totalScrollableRows : 0f;

                        float scrollBarTrackHeight = viewportHeight;
                        float scrollBarTravel = scrollBarTrackHeight - scrollBarHeight;

                        float scrollBarY = scrollFraction * scrollBarTravel;
                        float scrollBarCenter = scrollBarY + scrollBarHeight / 2f;

                        var initialY = CaretY + SCROLLER_WIDTH * Scale;

                        DrawScrollBar(sprites, Scale, initialY, viewportHeight, scrollBarCenter, scrollBarHeight);
                    }

                    int showCount = Math.Min(maxRows, items.Count);

                    for (int visIdx = start; visIdx < start + showCount; visIdx++)
                        DrawRow(sprites, items[visIdx], shouldScroll);
                }

                frame.AddRange(sprites);
            }
        }

        int GetMaxRowsFromSurface()
        {
            var max = ViewBox.Height - (CaretY - ViewBox.Y) - FooterHeight;
            var perLine = LINE_HEIGHT * Scale;
            return (int)(Math.Round(max / perLine - .5, MidpointRounding.AwayFromZero));
        }


        protected void DrawRow(List<MySprite> frame,
            KeyValuePair<MyItemType, double> item, bool showScrollBar)
        {
            string sprite;
            MyStringId locKey;

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

            if (!LocKeysCache.TryGetValue(item.Key, out locKey))
            {
                locKey = MyDefinitionManager.Static.TryGetPhysicalItemDefinition(item.Key).DisplayNameEnum ??
                         MyStringId.GetOrCompute(item.Key.TypeId);
                LocKeysCache[item.Key] = locKey;
            }

            var margin = ViewBox.Size.X * Margin;
            Vector2 position = ViewBox.Position;
            position.X += margin;
            position.Y = CaretY;

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

            frame.Add(MySprite.CreateClipRect(new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + (ViewBox.X) - 105 * Scale),
                (int)(position.Y + (LINE_HEIGHT + 5) * Scale))));

            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = MyTexts.GetString(locKey),
                Position = position,
                RotationOrScale = Scale,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });
            frame.Add(MySprite.CreateClearClipRect());
            position.X = ViewBox.Width + ViewBox.X - margin;
            if (showScrollBar)
                position.X -= SCROLLER_WIDTH * Scale;
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

            CaretY += LINE_HEIGHT * Scale;
        }

        protected void DrawScrollBar(List<MySprite> frame, float scale, float initialY, float viewportHeight,
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


        /// <summary>
        /// Draws a "capsule": a rectangle plus two half-circles.
        /// </summary>
        private void DrawCapsule(List<MySprite> frame, Vector2 center, int width, float height, Color color)
        {
            // Base rectangle
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareSimple",
                Position = center,
                Size = new Vector2(width, height + .5f),
                Color = color,
                Alignment = TextAlignment.CENTER
            });

            var capsSize = new Vector2(width);

            // Top cap (semicircle pointing down, rotation = 0)
            frame.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SemiCircle",
                Position = new Vector2(center.X, center.Y - height / 2f),
                Size = capsSize, // circle diameter
                RotationOrScale = 0f, // 0 rad → flat side up, round side down
                Color = color,
                Alignment = TextAlignment.CENTER
            });

            // Bottom cap (semicircle pointing up, rotation = π)
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "SemiCircle",
                Position = new Vector2(center.X, center.Y + height / 2f),
                Size = capsSize,
                RotationOrScale = (float)Math.PI, // flipped
                Color = color,
                Alignment = TextAlignment.CENTER
            });
        }


        protected override void DrawTitle(List<MySprite> frame)
        {
            var margin = ViewBox.Size.X * Margin;

            Vector2 position = ViewBox.Position;
            position.X += margin;
            position.Y += (ViewBox.Size.Y * Margin) / 2;

            CaretY = position.Y;

            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Textures\\FactionLogo\\Others\\OtherIcon_18.dds",
                Position = position + new Vector2(10f, 20) * Scale,
                Size = new Vector2(40 * Scale),
                Color = Config.HeaderColor,
                Alignment = TextAlignment.CENTER
            });
            position.X += ViewBox.Width / 8f;

            var stockText = MyTexts.Get(MyStringId.GetOrCompute("BlockPropertyTitle_Stockpile"));

            var endSize = Surface.MeasureStringInPixels(stockText, "White", Scale * 1.3f);

            var availableSize = new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + (ViewBox.X) - endSize.X - (2 * margin)),
                (int)(position.Y + TITLE_HEIGHT * Scale));
            frame.Add(MySprite.CreateClipRect(availableSize));

            StringBuilder displayNameSb = new StringBuilder();
            string displayName;

            foreach (var item in Config.SelectedCategories)
                displayNameSb.Append(ItemCategoryHelper.GetGroupDisplayName(item) + ", ");

            if (displayNameSb.Length == 0)
                displayName = MyTexts.GetString(Title);
            else
            {
                displayNameSb.Length--;
                displayNameSb.Length--;

                if (!TitleCache.TryGetValue(displayNameSb.ToString() + Scale, out displayName))
                {
                    TitleCache.Clear();

                    StringBuilder trimmedSb = new StringBuilder(displayNameSb.ToString());

                    TrimText(ref trimmedSb, availableSize.Width, 1.3f);

                    displayName = trimmedSb.ToString();
                    TitleCache[displayNameSb.ToString() + Scale] = displayName;
                }
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
            position.X = ViewBox.Width + ViewBox.X - margin;

            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = stockText.ToString(),
                Position = position,
                RotationOrScale = Scale * 1.3f,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            CaretY += 40 * Scale;
        }
        protected static string FormatItemQty(double input)
        {
            if (input >= 1000000000)
                // Congratulations, you've successfully created a singularity
                return (input / 1000000000d).ToString("0.00", CultureInfo.CurrentUICulture) + "G";
            if (input >= 1000000)
                return (input / 1000000d).ToString("0.00", CultureInfo.CurrentUICulture) + "M";
            if (input >= 10000)
                return (input / 1000d).ToString("0.00", CultureInfo.CurrentUICulture) + "k";

            return input.ToString("0.##", CultureInfo.CurrentUICulture);
        }
    }
}