using System.Collections.Generic;
using System.Globalization;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage;
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
        
        protected ItemCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        public override void Run()
        {
            base.Run();

            if (Config == null)
                return;
            
            DrawItems();
        }

        public void DrawItems()
        {
            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                DrawTitle(sprites, 1);

                var items = ReadItems(Block as IMyTerminalBlock);

                if (items.Count == 0)
                {
                    var margin = ViewBox.Size.X * Margin;
                    Vector2 position = ViewBox.Position;
                    position.X += margin;
                    position.Y = CaretY;
                    sprites.Add(MakeText((IMyTextSurface)Surface,
                        $"- {MyTexts.GetString("BlockPropertyProperties_WaterLevel_Empty")} -", position, 0.78f));
                }
                else
                {
                    //todo Re-Implement Scrolling

                    /*
                    int maxRows = GetMaxRowsFromSurface(pos.Y);
                    if (maxRows < 1) maxRows = 1;

                    bool shouldScroll = items.Count > (int)Math.Floor(maxRows * 0.95);
                    int visible = maxRows;
                    int start = 0;

                    if (shouldScroll && items.Count > visible)
                    {
                        int step = GetScrollStep(SCROLL_SECONDS);
                        start = step % items.Count;
                    }

                    int showCount = Math.Min(visible, items.Count);
                    for (int visIdx = 0; visIdx < showCount; visIdx++)
                    {
                        int realIdx = (start + visIdx) % items.Count;
                        var p = pos + new Vector2(0f, visIdx * LINE);
                        string line = items[realIdx].Key + ": " + FormatQty(items[realIdx].Value);
                        sprites.Add(Text(line, p, 0.78f));
                    }*/

                    foreach (var item in items)
                    {
                        DrawRow(sprites, 1, item);
                    }
                }

                frame.AddRange(sprites);
            }
        }
        
        
        protected void DrawRow(List<MySprite> frame, float scale,
            KeyValuePair<MyItemType, double> item)
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
                locKey = MyDefinitionManager.Static.TryGetPhysicalItemDefinition(item.Key).DisplayNameEnum ?? MyStringId.GetOrCompute(item.Key.TypeId);
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
                Position = position + new Vector2(10f, 15) * scale,
                Size = new Vector2(30 * scale),
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.CENTER
            });
            position.X += ViewBox.Width / 8f;

            frame.Add(MySprite.CreateClipRect(new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + (ViewBox.X) - 105 * scale),
                (int)(position.Y + 35 * scale))));
            
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = MyTexts.GetString(locKey),
                Position = position,
                RotationOrScale = scale,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });
            frame.Add(MySprite.CreateClearClipRect());
            position.X = ViewBox.Width + ViewBox.X - margin;
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = FormatItemQty(item.Value),
                Position = position,
                RotationOrScale = scale,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            CaretY += 30 * scale;
        }
        
        protected override void DrawTitle(List<MySprite> frame, float scale)
        {
            var margin = ViewBox.Size.X * Margin;

            Vector2 position = ViewBox.Position;
            position.X += margin;
            position.Y += (ViewBox.Size.Y * Margin) / 2;

            CaretY = position.Y;

            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Textures\\FactionLogo\\Others\\OtherIcon_5.dds",
                Position = position + new Vector2(10f, 20) * scale,
                Size = new Vector2(40 * scale),
                Color = Config.HeaderColor,
                Alignment = TextAlignment.CENTER
            });
            position.X += ViewBox.Width / 8f;
            frame.Add(MySprite.CreateClipRect(new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + (ViewBox.X) - 105 * scale),
                (int)(position.Y + 35 * scale))));
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = MyTexts.GetString(Title),
                Position = position,
                RotationOrScale = scale * 1.3f,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });
            frame.Add(MySprite.CreateClearClipRect());
            position.X = ViewBox.Width + ViewBox.X - margin;
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = MyTexts.GetString("BlockPropertyTitle_Stockpile"),
                Position = position,
                RotationOrScale = scale * 1.3f,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            CaretY += 40 * scale;
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