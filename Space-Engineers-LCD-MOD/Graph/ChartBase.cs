using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Graph.Data.Scripts.Graph.Sys;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Graph.Data.Scripts.Graph
{
    public abstract class ChartBase : MyTextSurfaceScriptBase
    {
        public static Dictionary<MyItemType, string> SpriteCache =
            new Dictionary<MyItemType, string>();

        public static Dictionary<MyItemType, MyStringId> LocKeysCache =
            new Dictionary<MyItemType, MyStringId>();

        public static Dictionary<IMyTerminalBlock, MyTuple<int, ScreenProviderConfig>> ActiveScreens =
            new Dictionary<IMyTerminalBlock, MyTuple<int, ScreenProviderConfig>>();

        List<KeyValuePair<MyItemType, double>> _itemsCache =
            new List<KeyValuePair<MyItemType, double>>();

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

        public ScreenConfig Config { get; protected set; }

        ScreenProviderConfig _providerConfig;

        public int SurfaceIndex { get; protected set; }

        protected ChartBase(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            UpdateViewBox();
        }

        public override void Dispose()
        {
            if (_providerConfig != null)
            {
                MyTuple<int, ScreenProviderConfig> config;
                if (ActiveScreens.TryGetValue((IMyTerminalBlock)Block, out config))
                {
                    config.Item1++;
                    if (config.Item1 == 0)
                    {
                        ActiveScreens.Remove((IMyTerminalBlock)Block);
                        Save((IMyEntity)Block, _providerConfig);
                    }
                }
            }

            base.Dispose();
        }

        public static void Save(IMyEntity storageEntity, ScreenProviderConfig providerConfig)
        {
            if (storageEntity.Storage != null)
                storageEntity.Storage[Constants.STORAGE_GUID] = Convert
                    .ToBase64String(MyAPIGateway.Utilities
                        .SerializeToBinary(providerConfig));
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
            if (Config == null)
            {
                GetSettings((IMyTextSurface)Surface, (IMyCubeBlock)Block);
                return;
            }

            if (Math.Abs(CurrentTextPadding - Surface.TextPadding) > .1f)
                UpdateViewBox();

            if (GridLogic == null)
                GridLogicSession.components.TryGetValue(Block.CubeGrid.EntityId, out GridLogic);

            base.Run();

            DrawItems();
        }

        void GetSettings(IMyTextSurface surface, IMyCubeBlock block)
        {
            IMyTextSurfaceProvider surfaceProvider = block as IMyTextSurfaceProvider;
            while (!surface.Equals(surfaceProvider.GetSurface(SurfaceIndex)) && SurfaceIndex < 32)
                SurfaceIndex++;

            if (SurfaceIndex < 32)
                LoadSettings(surface, block);
        }

        public void DrawItems()
        {
            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                DrawTitle(sprites, 1, Config.HeaderColor);

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

        protected virtual void DrawTitle(List<MySprite> frame, float scale, Color color)
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
                (int)(ViewBox.Width - position.X),
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

            CaretY += 40 * scale;
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

        protected static readonly Regex RxGroup = new Regex(@"\(\s*G\s*:\s*(.+?)\s*\)", RegexOptions.IgnoreCase);
        protected static readonly Regex RxContainer = new Regex(@"\(\s*(?!G\s*:)(.+?)\s*\)", RegexOptions.IgnoreCase);

        protected static MySprite MakeText(IMyTextSurface surf, string s, Vector2 p, float scale)
        {
            return new MySprite
            {
                Type = SpriteType.TEXT,
                Data = s,
                Position = p,
                Color = surf.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                RotationOrScale = scale
            };
        }

        protected static int GetScrollStep(int secondsPerStep)
        {
            try
            {
                var sess = MyAPIGateway.Session;
                if (sess == null) return 0;
                if (secondsPerStep <= 0) secondsPerStep = 1;
                double sec = sess.ElapsedPlayTime.TotalSeconds;
                return (int)(sec / secondsPerStep);
            }
            catch
            {
                return 0;
            }
        }

        protected static int GetMaxRows(IMyTextSurface surf, float listStartY, float lineHeight)
        {
            float surfH = 512f;
            try
            {
                surfH = surf.SurfaceSize.Y;
            }
            catch
            {
            }

            float available = Math.Max(0f, surfH - listStartY - 10f);
            int rows = (int)Math.Floor(available / Math.Max(1f, lineHeight));
            return rows < 1 ? 1 : rows;
        }

        protected static void ParseFilter(IMyTerminalBlock lcd, out string mode, out string token)
        {
            mode = null;
            token = null;
            if (lcd == null) return;
            var name = lcd.CustomName ?? string.Empty;

            var mg = RxGroup.Match(name);
            if (mg.Success)
            {
                mode = "group";
                token = mg.Groups[1].Value.Trim();
                return;
            }

            var mc = RxContainer.Match(name);
            if (mc.Success)
            {
                mode = "container";
                token = mc.Groups[1].Value.Trim();
            }
        }

        protected static readonly CultureInfo Pt = new CultureInfo("pt-BR");

        protected static string FormatQty(double v)
        {
            if (v >= 1000.0) return Math.Round(v).ToString("#,0", Pt);
            return v.ToString("0.##", Pt);
        }

        protected static List<KeyValuePair<string, double>> SortedItems(Dictionary<string, double> source)
        {
            var list = new List<KeyValuePair<string, double>>();
            if (source == null) return list;
            foreach (var kv in source) list.Add(kv);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            return list;
        }

        private void LoadSettings(IMyTextSurface surface, IMyCubeBlock block)
        {
            MyTuple<int, ScreenProviderConfig> config;

            if (ActiveScreens.TryGetValue((IMyTerminalBlock)block, out config))
            {
                _providerConfig = config.Item2;
                config.Item1++;
                Config = _providerConfig.Screens[SurfaceIndex];
            }
            else
            {
                var storageEntity = (IMyEntity)block;
                if (storageEntity.Storage == null)
                    storageEntity.Storage = new MyModStorageComponent();

                string value;
                if (storageEntity.Storage.TryGetValue(Constants.STORAGE_GUID, out value))
                {
                    try
                    {
                        _providerConfig =
                            MyAPIGateway.Utilities.SerializeFromBinary<ScreenProviderConfig>(
                                Convert.FromBase64String(value));
                        Config = _providerConfig.Screens[SurfaceIndex];
                    }
                    catch (Exception e)
                    {
                        MyAPIGateway.Utilities.ShowNotification("Fail to Load Settings");
                        MyLog.Default.Log(MyLogSeverity.Error, e.ToString());
                        CreateSettings(block);
                    }
                }
                else
                {
                    CreateSettings(block);
                }

                if (_providerConfig != null)
                {
                    if (!ActiveScreens.TryGetValue((IMyTerminalBlock)block, out config))
                    {
                        ActiveScreens[(IMyTerminalBlock)block] =
                            new MyTuple<int, ScreenProviderConfig>(0, _providerConfig);
                    }

                    config.Item1++;
                }
            }
        }

        private void CreateSettings(IMyCubeBlock block)
        {
            var lcd = block as IMyTextPanel;
            if (lcd != null)
            {
                _providerConfig = new ScreenProviderConfig(1);
                Config = _providerConfig.Screens[0];
                return;
            }

            _providerConfig = new ScreenProviderConfig(((IMyTextSurfaceProvider)block).SurfaceCount);
            Config = _providerConfig.Screens[SurfaceIndex];
        }
    }
}