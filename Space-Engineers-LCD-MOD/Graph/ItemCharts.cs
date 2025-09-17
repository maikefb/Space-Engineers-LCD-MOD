using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Graph.Data.Scripts.Graph.Sys;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Utils;
using VRageMath;

namespace Graph.Data.Scripts.Graph
{
    public abstract class ItemCharts : MyTextSurfaceScriptBase
    {
        public static Dictionary<MyItemType, string> SpriteCache = new Dictionary<MyItemType, string>();
        public static Dictionary<MyItemType, MyStringId> LocKeysCache = new Dictionary<MyItemType, MyStringId>();

        List<KeyValuePair<MyItemType, double>> _itemsCache = new List<KeyValuePair<MyItemType, double>>();

        /// <summary>
        /// Relative area of the <see cref="Sandbox.ModAPI.IMyTextSurface.TextureSize"/> That is Visible
        /// </summary>
        public RectangleF ViewBox { get; protected set; }

        protected GridLogic GridLogic;

        protected float CurrentTextPadding;

        protected float CaretY;

        protected float Margin = 0.02f;
        public abstract Dictionary<MyItemType, double> ItemSource { get; }
        public abstract string Title { get; protected set; }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        protected ItemCharts(Sandbox.ModAPI.Ingame.IMyTextSurface surface, VRage.Game.ModAPI.Ingame.IMyCubeBlock block,
            Vector2 size) : base(surface, block, size)
        {
            UpdateViewBox();
        }

        protected void UpdateViewBox()
        {
            var sizeOffset = (Surface.TextureSize - Surface.SurfaceSize) / 2;

            CurrentTextPadding = Surface.TextPadding;

            var padding = (Surface.TextPadding / 100) * Surface.SurfaceSize;
            sizeOffset += padding / 2;

            ViewBox = new RectangleF(sizeOffset.X, sizeOffset.Y, Surface.SurfaceSize.X - padding.X,
                Surface.SurfaceSize.Y - padding.Y);
        }

        public override void Run()
        {
            if (Math.Abs(CurrentTextPadding - Surface.TextPadding) > .1f)
                UpdateViewBox();

            if (GridLogic == null)
                GridLogicSession.components.TryGetValue(Block.CubeGrid.EntityId, out GridLogic);

            base.Run();

            DrawItems();
        }

        public void DrawItems()
        {
            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                DrawTitle(sprites, 0.95f, Color.Red);

                var items = ReadItems(Block as IMyTerminalBlock);

                if (items.Count == 0)
                {
                    var margin = ViewBox.Size.X * Margin;
                    Vector2 position = ViewBox.Position;
                    position.X += margin;
                    position.Y = CaretY;
                    sprites.Add(Text($"- {MyTexts.GetString("BlockPropertyProperties_WaterLevel_Empty")} -", position, 0.78f));
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

        protected List<KeyValuePair<MyItemType, double>> ReadItems(IMyTerminalBlock lcd)
        {
            _itemsCache.Clear();
            if (lcd == null || ItemSource == null)
                return _itemsCache;

            foreach (var keyValuePair in ItemSource)
                _itemsCache.Add(keyValuePair);

            _itemsCache.Sort((a, b) => b.Value.CompareTo(a.Value));
            return _itemsCache;
        }

        protected void DrawTitle(List<MySprite> frame, float scale, Color color)
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
                Color = color,
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
                Color = color,
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
                Color = color,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            CaretY += 40 * scale;
        }

        protected void DrawRow(List<MySprite> frame, float scale, KeyValuePair<MyItemType, double> item)
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
                var name = item.Key.ToString().Substring(16).Split('/');
                locKey = MyStringId.TryGet("DisplayName_Item_" + name[1] + name[0]);
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

            var itemName = locKey == MyStringId.NullOrEmpty
                ? item.Key.ToString().Split('/')[1]
                : MyTexts.GetString(locKey);

            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = itemName,
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
                Data = FormatQty(item.Value),
                Position = position,
                RotationOrScale = scale,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            CaretY += 30 * scale;
        }


        private string FormatQty(double v)
        {
            if (v >= 1000) return Math.Round(v).ToString("#,0", new CultureInfo("pt-BR"));
            return v.ToString("0.##", new CultureInfo("pt-BR"));
        }
        
        private MySprite Text(string s, Vector2 p, float scale)
        {
            return new MySprite { Type = SpriteType.TEXT, Data = s, Position = p, Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = scale };
        }
    }
}