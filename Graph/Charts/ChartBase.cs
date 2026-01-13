using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Graph.Helpers;
using Graph.System;
using Graph.System.Config;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage;
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
        public static List<ChartBase> Instances = new List<ChartBase>();

        public IMyFaction Faction { get; protected set; }
        protected string Icon { get; set; }

        protected virtual SortMethod SortMethod => Config.SortMethod;

        List<KeyValuePair<MyItemType, double>> _itemsCache = new List<KeyValuePair<MyItemType, double>>();

        /// <summary>
        /// Relative area of the <see cref="Sandbox.ModAPI.IMyTextSurface.TextureSize"/> That is Visible
        /// </summary>
        public RectangleF ViewBox { get; protected set; }

        protected GridLogic GridLogic;

        protected float CaretY;
        protected float FooterHeight;

        protected float Margin = 0.02f;
        public abstract Dictionary<MyItemType, double> ItemSource { get; }
        public virtual string Title => DefaultTitle;
        protected virtual string DefaultTitle => "|";

        protected float Scale = 1;

        float _userScale;
        float _userPadding;
        string _languageWord;
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

        void DrawSplash()
        {
            var offset = Math.Min(ViewBox.Width, ViewBox.Height) / 5;
            var frame = Surface.DrawFrame();
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", ViewBox.Center,
                new Vector2(Math.Max(ViewBox.Width, ViewBox.Height)*2), FactionHelper.GetBackgroundColor(Faction)));
            frame.Add(new MySprite(SpriteType.TEXTURE, Icon, new Vector2(ViewBox.Center.X,ViewBox.Center.Y - offset/2),
                new Vector2(Math.Min(ViewBox.Width, ViewBox.Height)/1.5f), FactionHelper.GetIconColor(Faction)));
            frame.Add(new MySprite(SpriteType.TEXT, Title, new Vector2(ViewBox.Center.X, ViewBox.Center.Y + offset),
                null,FactionHelper.GetIconColor(Faction), "White", rotation:1.6f));
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
                if (ProviderConfig != null)
                    ConfigManager.Save((IMyEntity)Block, ProviderConfig);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }

            Instances.Remove(this);
            base.Dispose();
        }

        protected void UpdateViewBox()
        {
            var sizeOffset = (Surface.TextureSize - Surface.SurfaceSize) / 2;

            _userPadding = Surface.TextPadding;

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

        protected List<KeyValuePair<MyItemType, double>> ReadItems(IMyTerminalBlock lcd)
        {
            _itemsCache.Clear();
            if (lcd == null || ItemSource == null)
                return _itemsCache;

            foreach (var keyValuePair in ItemSource)
                _itemsCache.Add(keyValuePair);

            
            switch (SortMethod)
            {
                case SortMethod.Type:
                    _itemsCache.Sort((a, b) =>
                    {
                        return a.Key.TypeId != b.Key.TypeId ? 
                            string.Compare(a.Key.TypeId, b.Key.TypeId, StringComparison.CurrentCulture) : 
                            string.Compare(a.Key.SubtypeId, b.Key.SubtypeId, StringComparison.CurrentCulture);
                    });
                    break;
                default:
                    _itemsCache.Sort((a, b) => b.Value.CompareTo(a.Value));
                    break;
            }

            return _itemsCache;
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
            
            if(!TitleVisible)
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

            frame.Add(MySprite.CreateClipRect(new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + ViewBox.X),
                (int)(position.Y + 35 * Scale))));

            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = MyTexts.GetString(Title),
                Position = position,
                RotationOrScale = Scale * 1.3f,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            frame.Add(MySprite.CreateClearClipRect());

            CaretY += 40 * Scale;
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

        protected void TrimText(ref StringBuilder sb, float availableWidth, float fontSize = 1)
        {
            Vector2 textSize = Surface.MeasureStringInPixels(sb, "White", fontSize * Scale);

            if (textSize.X > availableWidth)
            {
                const string ELLIPSIS = "...";

                for (int i = sb.Length - 1; i > 0; i--)
                {
                    sb.Length = i; 
                    sb.Append(ELLIPSIS); 
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

        protected static readonly CultureInfo Pt = new CultureInfo("pt-BR");

        protected string Pow(double watts)
        {
            CultureInfo culture = new CultureInfo("pt-BR");

            double a = global::System.Math.Abs(watts);
            string sign = watts < 0 ? "-" : "";

            if (a < 1e-12)
                return "0 W";

            if (a >= 1e24) return sign + (a / 1e24).ToString("0.##", culture) + " YW";
            if (a >= 1e21) return sign + (a / 1e21).ToString("0.##", culture) + " ZW";
            if (a >= 1e18) return sign + (a / 1e18).ToString("0.##", culture) + " EW";
            if (a >= 1e15) return sign + (a / 1e15).ToString("0.##", culture) + " PW";
            if (a >= 1e12) return sign + (a / 1e12).ToString("0.##", culture) + " TW";
            if (a >= 1e9) return sign + (a / 1e9).ToString("0.##", culture) + " GW";
            if (a >= 1e6) return sign + (a / 1e6).ToString("0.##", culture) + " MW";
            if (a >= 1e3) return sign + (a / 1e3).ToString("0.##", culture) + " kW";
            if (a >= 1.0) return sign + a.ToString("0.##", culture) + " W";
            if (a >= 1e-3) return sign + (a / 1e-3).ToString("0.##", culture) + " mW";
            if (a >= 1e-6) return sign + (a / 1e-6).ToString("0.##", culture) + " uW";
            if (a >= 1e-9) return sign + (a / 1e-9).ToString("0.##", culture) + " nW";
            if (a >= 1e-12) return sign + (a / 1e-12).ToString("0.##", culture) + " pW";
            return sign + a.ToString("0.##", culture) + " W";
        }


        protected string PowForce(double newtons)
        {
            var culture = new CultureInfo("pt-BR");

            double a = global::System.Math.Abs(newtons);
            string sign = newtons < 0 ? "-" : "";

            if (a < 1e-12)
                return "0 N";

            if (a >= 1e24) return sign + (a / 1e24).ToString("0.##", culture) + " YN";
            if (a >= 1e21) return sign + (a / 1e21).ToString("0.##", culture) + " ZN";
            if (a >= 1e18) return sign + (a / 1e18).ToString("0.##", culture) + " EN";
            if (a >= 1e15) return sign + (a / 1e15).ToString("0.##", culture) + " PN";
            if (a >= 1e12) return sign + (a / 1e12).ToString("0.##", culture) + " TN";
            if (a >= 1e9) return sign + (a / 1e9).ToString("0.##", culture) + " GN";
            if (a >= 1e6) return sign + (a / 1e6).ToString("0.##", culture) + " MN";
            if (a >= 1e3) return sign + (a / 1e3).ToString("0.##", culture) + " kN";
            if (a >= 1e-3) return sign + (a / 1e-3).ToString("0.##", culture) + " mN";
            if (a >= 1e-6) return sign + (a / 1e-6).ToString("0.##", culture) + " uN";
            if (a >= 1e-9) return sign + (a / 1e-9).ToString("0.##", culture) + " nN";
            return sign + a.ToString("0.##", culture) + " N";
        }


        protected string Pct(float f)
        {
            return ((int)Math.Round(f * 100f)).ToString(Pt) + "%";
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
            Scale = GetAutoScaleUniform();
            UpdateViewBox();
        }

        public void UpdateFaction(IMyFaction faction)
        {
            Faction = faction;
            Icon = FactionHelper.GetIcon(faction);
            FactionHelper.GetIcon(faction);
        }
    }
}