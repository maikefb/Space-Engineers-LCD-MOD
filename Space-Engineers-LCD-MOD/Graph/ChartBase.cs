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
                GridLogicSession.Components.TryGetValue(Block.CubeGrid.EntityId, out GridLogic);

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
                    LoadSettings(block, index);
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

            _itemsCache.Sort((a, b) => b.Value.CompareTo(a.Value));
            return _itemsCache;
        }

        protected virtual void DrawTitle(List<MySprite> frame, float scale)
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
                (int)(ViewBox.Width - position.X + ViewBox.X),
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

            CaretY += 40 * scale;
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
            return new MySprite { Type = SpriteType.TEXT, Data = s, Position = p,
                Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = scale };
        }
        protected MySprite Centered(string s, Vector2 p, float scale)
        {
            return new MySprite { Type = SpriteType.TEXT, Data = s, Position = p,
                Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.CENTER, RotationOrScale = scale };
        }

        protected static readonly CultureInfo Pt = new CultureInfo("pt-BR");

        protected string Pow(double mw)
        {
            double a = Math.Abs(mw);
            string sign = mw < 0 ? "-" : "";
            if (a >= 1000000.0) return sign + (a/1000000.0).ToString("0.##", Pt) + " MW";
            if (a >= 1.0)       return sign + a.ToString("0.##", Pt) + " MW";
            return sign + (a*1000.0).ToString("0.##", Pt) + " kW";
        }
        protected string Pct(float f) { return ((int)Math.Round(f * 100f)).ToString(Pt) + "%"; }

        private void LoadSettings(IMyCubeBlock block, int index)
        {
            MyTuple<int, ScreenProviderConfig> config;

            if (ActiveScreens.TryGetValue((IMyTerminalBlock)block, out config))
            {
                _providerConfig = config.Item2;
                config.Item1++;
                Config = _providerConfig.Screens[index];
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
                        
                        if(_providerConfig.ParentGrid != block.CubeGrid.EntityId)
                            _providerConfig.ParentGrid = block.CubeGrid.EntityId;
                        
                        Config = _providerConfig.Screens[index];
                    }
                    catch (Exception e)
                    {
                        MyAPIGateway.Utilities.ShowNotification("Fail to Load Settings");
                        MyLog.Default.Log(MyLogSeverity.Error, e.ToString());
                        CreateSettings(block, index);
                    }
                }
                else
                {
                    CreateSettings(block, index);
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

        private void CreateSettings(IMyCubeBlock block, int index)
        {
            var lcd = block as IMyTextPanel;
            if (lcd != null)
            {
                _providerConfig = new ScreenProviderConfig(1, block.CubeGrid.EntityId);
                Config = _providerConfig.Screens[0];
                return;
            }

            _providerConfig = new ScreenProviderConfig(((IMyTextSurfaceProvider)block).SurfaceCount, block.CubeGrid.EntityId);
            Config = _providerConfig.Screens[index];
        }
        
        protected Vector2 GetAutoScale2D(float logicalWidth = 512f, float logicalHeight = 512f)
        {
            if (logicalWidth  <= 0f) logicalWidth  = 512f;
            if (logicalHeight <= 0f) logicalHeight = 512f;
            return new Vector2(ViewBox.Size.X / logicalWidth, ViewBox.Size.Y / logicalHeight);
        }

        protected float GetAutoScaleUniform(float logicalWidth = 512f, float logicalHeight = 512f)
        {
            var s = GetAutoScale2D(logicalWidth, logicalHeight);
            return Math.Min(s.X, s.Y);
        }

        
    }
}