using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Graph.Extensions;
using Graph.Helpers;
using Graph.Panels;
using Graph.System;
using Graph.System.Config;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Graph.Charts
{
    public abstract class ChartBase : MyTextSurfaceScriptBase
    {
        static Dictionary<string, Vector2> _fontSizeCache = new Dictionary<string, Vector2>();
        static Dictionary<MyDefinitionId, MyItemType> _typeCache = new Dictionary<MyDefinitionId, MyItemType>();
        static StringBuilder _stringBuilderBuffer = new StringBuilder();

        public static List<ChartBase> Instances = new List<ChartBase>();

        public IMyFaction Faction { get; protected set; }
        protected string Icon { get; set; }

        protected virtual SortMethod SortMethod => Config.SortMethod;

        Dictionary<MyItemType, double> _itemsCache = new Dictionary<MyItemType, double>();

        /// <summary>
        /// Relative area of the <see cref="Sandbox.ModAPI.IMyTextSurface.TextureSize"/> That is Visible
        /// </summary>
        public RectangleF ViewBox { get; protected set; }

        protected GridLogic GridLogic;

        protected float CaretY;
        protected float FooterHeight;

        protected float Margin = 0.02f;

        protected const float TitleBarHeightBase = 40f;
        public abstract Dictionary<MyItemType, double> ItemSource { get; }
        public virtual string Title => DefaultTitle;
        protected virtual string DefaultTitle => "|";

        protected float Scale = 1;

        float _userScale;
        float _userPadding;
        string _languageWord;
        string _cachedTitleSource;
        string _cachedTitleText;
        float _cachedTitleAvailableWidth = -1f;
        float _cachedTitleFontSize = -1f;
        bool _cachedTitleLocalized;
        public bool TitleVisible { get; private set; } = true;
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        public ScreenConfig Config { get; protected set; }

        public bool Dirty => _dirty;
        bool _dirty;

        public ScreenProviderConfig ProviderConfig;

        protected ChartBase(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Instances.Add(this);
            UpdateViewBox();
            UpdateFaction(FactionHelper.GetOwnerFaction(Block as IMyTerminalBlock));
            DrawSplash();
        }


        public static Vector2 GetSizeInPixel(string text, string font, float fontSize,
            Sandbox.ModAPI.Ingame.IMyTextSurface surface)
        {
            Vector2 size;
            var key = text + font + fontSize;
            if (_fontSizeCache.TryGetValue(key, out size)) return size;
            _stringBuilderBuffer.Clear();
            _stringBuilderBuffer.Append(text);
            size = surface.MeasureStringInPixels(_stringBuilderBuffer, font, fontSize);
            _fontSizeCache[key] = size;
            return size;
        }

        void DrawSplash()
        {
            var offset = Math.Min(ViewBox.Width, ViewBox.Height) / 5;
            var frame = Surface.DrawFrame();
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", ViewBox.Center,
                new Vector2(Math.Max(ViewBox.Width, ViewBox.Height) * 2), FactionHelper.GetBackgroundColor(Faction)));
            frame.Add(new MySprite(SpriteType.TEXTURE, Icon,
                new Vector2(ViewBox.Center.X, ViewBox.Center.Y - offset / 2),
                new Vector2(Math.Min(ViewBox.Width, ViewBox.Height) / 1.5f), FactionHelper.GetIconColor(Faction)));
            frame.Add(new MySprite(SpriteType.TEXT, Title, new Vector2(ViewBox.Center.X, ViewBox.Center.Y + offset),
                null, FactionHelper.GetIconColor(Faction), "White", rotation: 1.6f));
            frame.Dispose();
        }

        public void RequestRedraw()
        {
            LayoutChanged();
            _dirty = true;
            Run();
            _dirty = false;
        }

        public override void Dispose()
        {
            try
            {
                if (Block != null && ProviderConfig != null)
                    ConfigManager.Save((IMyEntity)Block, ProviderConfig);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }

            Instances.Remove(this);
            base.Dispose();
        }

        const float ServerExtraPadding = 4f;

        protected void UpdateViewBox()
        {
            var sizeOffset = (Surface.TextureSize - Surface.SurfaceSize) / 2;

            _userPadding = Surface.TextPadding;

            var padding = (Surface.TextPadding / 100) * Surface.SurfaceSize;
            sizeOffset += padding / 2;

            if (MyAPIGateway.Session != null && MyAPIGateway.Session.IsServer)
            {
                sizeOffset += new Vector2(ServerExtraPadding, ServerExtraPadding);
                ViewBox = new RectangleF(
                    sizeOffset.X, sizeOffset.Y,
                    Surface.SurfaceSize.X - padding.X - ServerExtraPadding * 2,
                    Surface.SurfaceSize.Y - padding.Y - ServerExtraPadding * 2);
            }
            else
            {
                ViewBox = new RectangleF(sizeOffset.X, sizeOffset.Y, Surface.SurfaceSize.X - padding.X,
                    Surface.SurfaceSize.Y - padding.Y);
            }
        }

        public override void Run()
        {
            if (Config == null)
            {
                GetSettings((IMyTextSurface)Surface, (IMyCubeBlock)Block);
                return;
            }

            if (Math.Abs(_userPadding - Surface.TextPadding) > .01f ||
                Math.Abs(_userScale - Config.Scale) > .001f ||
                TitleVisible != Config.TitleVisible ||
                _languageWord != MyTexts.GetString("Language"))
                LayoutChanged();

            if (GridLogic == null)
                LcdModSessionComponent.Components.TryGetValue(Block.CubeGrid.EntityId, out GridLogic);

            base.Run();
        }

        void GetSettings(IMyTextSurface surface, IMyCubeBlock block)
        {
            var index = 0;
            IMyTextSurfaceProvider surfaceProvider = (IMyTextSurfaceProvider)block;
            while (index < surfaceProvider.SurfaceCount)
            {
                if (surface.Equals(surfaceProvider.GetSurface(index)))
                {
                    ScreenConfig config;
                    ConfigManager.LoadSettings(block, index, ref ProviderConfig, out config);
                    Config = config;
                    return;
                }

                index++;
            }
        }

        protected virtual List<KeyValuePair<MyItemType, double>> ReadItems(IMyTerminalBlock lcd)
        {
            if (Config.HideEmpty || Config.SelectedItems.Any())
                _itemsCache.Clear();

            if (lcd == null || ItemSource == null)
                return new List<KeyValuePair<MyItemType, double>>();

            if (_itemsCache.Any())
            {
                var ar = _itemsCache.Keys.ToArray();
                foreach (var key in ar) // will be 0 unless Clear() was NOT called
                    _itemsCache[key] = 0;
            }


            if (!Config.HideEmpty)
            {
                foreach (var configSelectedItem in Config.SelectedItems)
                {
                    MyItemType type;
                    if (!_typeCache.TryGetValue(configSelectedItem, out type))
                    {
                        type = MyItemType.Parse(configSelectedItem.ToString());
                        _typeCache[configSelectedItem] = type;
                    }

                    _itemsCache[type] = 0;
                }
            }

            foreach (var keyValuePair in ItemSource)
                _itemsCache[keyValuePair.Key] = (keyValuePair.Value);


            switch (SortMethod)
            {
                case SortMethod.Type:
                    var sortedByType = new SortedDictionary<MyItemType, double>(ItemTypeComparer.Instance);
                    foreach (var entry in _itemsCache)
                    {
                        sortedByType[entry.Key] = entry.Value;
                    }

                    return sortedByType.ToList();
                default:
                    var sortedByValue = new SortedDictionary<double, List<KeyValuePair<MyItemType, double>>>(
                        DescendingDoubleComparer.Instance);
                    foreach (var entry in _itemsCache)
                    {
                        List<KeyValuePair<MyItemType, double>> bucket;
                        if (!sortedByValue.TryGetValue(entry.Value, out bucket))
                        {
                            bucket = new List<KeyValuePair<MyItemType, double>>();
                            sortedByValue[entry.Value] = bucket;
                        }

                        bucket.Add(entry);
                    }

                    return sortedByValue.SelectMany(b => b.Value).ToList();
                    ;
            }
        }

        sealed class ItemTypeComparer : IComparer<MyItemType>
        {
            public static readonly ItemTypeComparer Instance = new ItemTypeComparer();

            public int Compare(MyItemType a, MyItemType b)
            {
                int typeCmp = string.Compare(a.TypeId, b.TypeId, StringComparison.CurrentCulture);
                if (typeCmp != 0)
                    return typeCmp;
                return string.Compare(a.SubtypeId, b.SubtypeId, StringComparison.CurrentCulture);
            }
        }

        sealed class DescendingDoubleComparer : IComparer<double>
        {
            public static readonly DescendingDoubleComparer Instance = new DescendingDoubleComparer();

            public int Compare(double a, double b) => b.CompareTo(a);
        }

        /// <summary>
        /// Resets the <see cref="CaretY"/> to the Top of the screen, if <see cref="TitleVisible"/>, draws the Tittle 
        /// </summary>
        /// <param name="frame"></param>
        protected virtual void DrawTitle(List<MySprite> frame)
        {
            var margin = ViewBox.Size.X * Margin;
            Vector2 position = ViewBox.Position;
            position.X += margin;
            position.Y += (ViewBox.Size.Y * Margin) / 2;

            CaretY = position.Y;

            if (!TitleVisible)
                return;

            AddHeaderSprite(frame, new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = Icon,
                Position = position + new Vector2(20) * Scale,
                Size = new Vector2(40 * Scale),
                Color = Config.HeaderColor,
                Alignment = TextAlignment.CENTER
            });
            position.X += ViewBox.Width / 8f;

            frame.Add(MySprite.CreateClipRect(new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + ViewBox.X),
                (int)(position.Y + 35 * Scale))));

            var availableWidth = ViewBox.Width - position.X + ViewBox.X;
            var titleText = GetCachedTitleText(availableWidth, 1.3f, true);

            AddHeaderSprite(frame, new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = titleText,
                Position = position,
                RotationOrScale = Scale * 1.3f,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            frame.Add(MySprite.CreateClearClipRect());

            CaretY += TitleBarHeightBase * Scale;
        }

        protected virtual void DrawFooter(List<MySprite> frame)
        {
        }

        protected static readonly Regex RxGroup = new Regex(@"\(\s*G\s*:\s*(.+?)\s*\)", RegexOptions.IgnoreCase);
        protected static readonly Regex RxContainer = new Regex(@"\(\s*(?!G\s*:)(.+?)\s*\)", RegexOptions.IgnoreCase);

        protected static MySprite MakeText(IMyTextSurface surf, string s, Vector2 p, float scale,
            TextAlignment alignment = TextAlignment.LEFT)
        {
            return new MySprite
            {
                Type = SpriteType.TEXT,
                Data = s,
                Position = p,
                Color = surf.ScriptForegroundColor,
                Alignment = alignment,
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
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"[LCDMod] GetScrollStep error: {ex.Message}");
                return 0;
            }
        }


        protected virtual RectangleF GetCellViewBox(float xStart, float xEnd, float yStart, float cellHeight,
            float cellPadding)
        {
            var innerLeft = xStart + cellPadding;
            var innerRight = xEnd - cellPadding;
            var innerTop = yStart + cellPadding;
            var innerBottom = yStart + cellHeight - cellPadding;
            return new RectangleF(innerLeft, innerTop, innerRight - innerLeft, innerBottom - innerTop);
        }

        protected virtual MyTuple<RectangleF, RectangleF, RectangleF> GetCellSlots(float innerLeft, float innerRight,
            float innerTop, float innerBottom, float spacing)
        {
            var topRowHeight = spacing * Scale;
            var bottomRowTop = innerTop + topRowHeight;
            var bottomRowHeight = Math.Max(0f, innerBottom - bottomRowTop);
            var iconSize = innerBottom - innerTop;
            var contentLeft = innerLeft + iconSize;
            var contentWidth = Math.Max(0f, innerRight - contentLeft);

            var iconRect = new RectangleF(innerLeft, innerTop, iconSize, iconSize);
            var numberRect = new RectangleF(contentLeft, innerTop, contentWidth, topRowHeight);
            var nameRect = new RectangleF(contentLeft, bottomRowTop, contentWidth, bottomRowHeight);
            return new MyTuple<RectangleF, RectangleF, RectangleF>(iconRect, numberRect, nameRect);
        }

        protected virtual void DrawCellBackground(List<MySprite> frame, KeyValuePair<MyItemType, double> item,
            float xStart, float xEnd, float yStart, float cellHeight, float cellPadding)
        {
            var rl = xStart + cellPadding / 2;
            var rr = xEnd - cellPadding / 2;
            var rt = yStart + cellPadding / 2;
            var rb = yStart + cellHeight - cellPadding / 2;

            var backgroundColor = item.Value == 0 ? Config.ErrorColor: Config.HeaderColor;
            var a = backgroundColor.MulValue(0.2f);
            var cellRect = new RectangleF(rl, rt, rr - rl, rb - rt);
            var dropShadow = new RectangleF(cellRect.Position + 2, cellRect.Size);
            RectanglePanel.CreateSpritesFromRect(dropShadow, frame, a, .2f);
            RectanglePanel.CreateSpritesFromRect(cellRect, frame, backgroundColor, .2f);
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

        protected void TrimText(ref StringBuilder sb, float availableWidth, float fontSize = 1)
        {
            Vector2 textSize = Surface.MeasureStringInPixels(sb, "White", fontSize * Scale);

            if (textSize.X > availableWidth)
            {
                const string ellipsis = "...";

                for (int i = sb.Length - 1; i > 0; i--)
                {
                    sb.Length = i;
                    sb.Append(ellipsis);
                    textSize = Surface.MeasureStringInPixels(sb, "White", fontSize * Scale);

                    if (textSize.X <= availableWidth)
                        break;

                    sb.Length = i;
                }
            }
        }

        protected static string FormatQty(double v)
        {
            if (v >= 1000.0) return Math.Round(v).ToString("#,0", CultureInfo.CurrentUICulture);
            return v.ToString("0.##", CultureInfo.CurrentUICulture);
        }

        protected static List<KeyValuePair<string, double>> SortedItems(Dictionary<string, double> source)
        {
            var list = new List<KeyValuePair<string, double>>();
            if (source == null) return list;
            foreach (var kv in source) list.Add(kv);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            return list;
        }

        protected Vector2 ToScreenMargin(Vector2 absoluteCenterInViewBox)
        {
            return new Vector2(absoluteCenterInViewBox.X, 512f - absoluteCenterInViewBox.Y);
        }

        protected MySprite Text(string s, Vector2 p, float scale)
        {
            return new MySprite
            {
                Type = SpriteType.TEXT, Data = s, Position = p,
                Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = scale
            };
        }

        protected MySprite Centered(string s, Vector2 p, float scale)
        {
            return new MySprite
            {
                Type = SpriteType.TEXT, Data = s, Position = p,
                Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.CENTER, RotationOrScale = scale
            };
        }

        protected string Pow(double watts)
        {
            double a = Math.Abs(watts);
            string sign = watts < 0 ? "-" : "";

            if (a < 1e-12)
                return "0 W";

            if (a >= 1e24) return sign + (a / 1e24).ToString("0.##", CultureInfo.CurrentUICulture) + " YW";
            if (a >= 1e21) return sign + (a / 1e21).ToString("0.##", CultureInfo.CurrentUICulture) + " ZW";
            if (a >= 1e18) return sign + (a / 1e18).ToString("0.##", CultureInfo.CurrentUICulture) + " EW";
            if (a >= 1e15) return sign + (a / 1e15).ToString("0.##", CultureInfo.CurrentUICulture) + " PW";
            if (a >= 1e12) return sign + (a / 1e12).ToString("0.##", CultureInfo.CurrentUICulture) + " TW";
            if (a >= 1e9) return sign + (a / 1e9).ToString("0.##", CultureInfo.CurrentUICulture) + " GW";
            if (a >= 1e6) return sign + (a / 1e6).ToString("0.##", CultureInfo.CurrentUICulture) + " MW";
            if (a >= 1e3) return sign + (a / 1e3).ToString("0.##", CultureInfo.CurrentUICulture) + " kW";
            if (a >= 1.0) return sign + a.ToString("0.##", CultureInfo.CurrentUICulture) + " W";
            if (a >= 1e-3) return sign + (a / 1e-3).ToString("0.##", CultureInfo.CurrentUICulture) + " mW";
            if (a >= 1e-6) return sign + (a / 1e-6).ToString("0.##", CultureInfo.CurrentUICulture) + " uW";
            if (a >= 1e-9) return sign + (a / 1e-9).ToString("0.##", CultureInfo.CurrentUICulture) + " nW";
            if (a >= 1e-12) return sign + (a / 1e-12).ToString("0.##", CultureInfo.CurrentUICulture) + " pW";
            return sign + a.ToString("0.##", CultureInfo.CurrentUICulture) + " W";
        }


        protected string PowForce(double newtons)
        {
            double a = Math.Abs(newtons);
            string sign = newtons < 0 ? "-" : "";

            if (a < 1e-12)
                return "0 N";

            if (a >= 1e24) return sign + (a / 1e24).ToString("0.##", CultureInfo.CurrentUICulture) + " YN";
            if (a >= 1e21) return sign + (a / 1e21).ToString("0.##", CultureInfo.CurrentUICulture) + " ZN";
            if (a >= 1e18) return sign + (a / 1e18).ToString("0.##", CultureInfo.CurrentUICulture) + " EN";
            if (a >= 1e15) return sign + (a / 1e15).ToString("0.##", CultureInfo.CurrentUICulture) + " PN";
            if (a >= 1e12) return sign + (a / 1e12).ToString("0.##", CultureInfo.CurrentUICulture) + " TN";
            if (a >= 1e9) return sign + (a / 1e9).ToString("0.##", CultureInfo.CurrentUICulture) + " GN";
            if (a >= 1e6) return sign + (a / 1e6).ToString("0.##", CultureInfo.CurrentUICulture) + " MN";
            if (a >= 1e3) return sign + (a / 1e3).ToString("0.##", CultureInfo.CurrentUICulture) + " kN";
            if (a >= 1e-3) return sign + (a / 1e-3).ToString("0.##", CultureInfo.CurrentUICulture) + " mN";
            if (a >= 1e-6) return sign + (a / 1e-6).ToString("0.##", CultureInfo.CurrentUICulture) + " uN";
            if (a >= 1e-9) return sign + (a / 1e-9).ToString("0.##", CultureInfo.CurrentUICulture) + " nN";
            return sign + a.ToString("0.##", CultureInfo.CurrentUICulture) + " N";
        }


        protected string Pct(float f)
        {
            return f.ToString("P0", CultureInfo.CurrentUICulture);
        }

        protected Vector2 GetAutoScale2D(float logicalWidth = 512f, float logicalHeight = 512f)
        {
            if (logicalWidth <= 0f) logicalWidth = 512f;
            if (logicalHeight <= 0f) logicalHeight = 512f;
            return new Vector2(ViewBox.Size.X / logicalWidth, ViewBox.Size.Y / logicalHeight);
        }

        protected float GetAutoScaleUniform(float logicalWidth = 512f, float logicalHeight = 512f)
        {
            var s = GetAutoScale2D(logicalWidth, logicalHeight);
            return Math.Min(s.X, s.Y) * Config.Scale;
        }

        protected virtual void LayoutChanged()
        {
            _userPadding = Surface.TextPadding;
            _userScale = Config.Scale;
            TitleVisible = Config.TitleVisible;
            _languageWord = MyTexts.GetString("Language");
            InvalidateTitleCache();
            Scale = GetAutoScaleUniform();
            UpdateViewBox();
        }

        protected string GetCachedTitleText(float availableWidth, float fontSize = 1.3f, bool localizeTitle = false)
        {
            var source = localizeTitle ? MyTexts.GetString(Title) : Title;
            availableWidth = Math.Max(0f, availableWidth);

            if (_cachedTitleText != null &&
                _cachedTitleSource == source &&
                _cachedTitleLocalized == localizeTitle &&
                Math.Abs(_cachedTitleAvailableWidth - availableWidth) <= 0.1f &&
                Math.Abs(_cachedTitleFontSize - fontSize) <= 0.0001f)
            {
                return _cachedTitleText;
            }

            var sb = new StringBuilder(source ?? string.Empty);
            if (availableWidth > 0f)
                TrimText(ref sb, availableWidth, fontSize);

            _cachedTitleSource = source;
            _cachedTitleLocalized = localizeTitle;
            _cachedTitleAvailableWidth = availableWidth;
            _cachedTitleFontSize = fontSize;
            _cachedTitleText = sb.ToString();
            return _cachedTitleText;
        }

        protected void InvalidateTitleCache()
        {
            _cachedTitleSource = null;
            _cachedTitleText = null;
            _cachedTitleAvailableWidth = -1f;
            _cachedTitleFontSize = -1f;
            _cachedTitleLocalized = false;
        }

        protected static void AddHeaderSprite(List<MySprite> frame, MySprite sprite)
        {
            frame.Add(sprite.Shadow(1f));
            frame.Add(sprite);
        }

        public void UpdateFaction(IMyFaction faction)
        {
            Faction = faction;
            Icon = FactionHelper.GetIcon(faction);
            FactionHelper.GetIcon(faction);
        }
    }
}