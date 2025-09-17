// /Graph/ItemCharts.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using Graph.Data.Scripts.Graph.Sys;

using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;                     // IMyTextSurface, MyAPIGateway

using VRage.Game.ModAPI;                  // IMyCubeBlock, IMyTerminalBlock
using VRage.Game.GUI.TextPanel;           // MySprite, SpriteType, TextAlignment  <-- IMPORTANTE
using VRageMath;

namespace Graph.Data.Scripts.Graph
{
    public abstract class ItemCharts : MyTextSurfaceScriptBase
    {
        /// <summary>
        /// Relative area of the <see cref="Sandbox.ModAPI.IMyTextSurface.TextureSize"/> That is Visible
        /// </summary>
        public readonly RectangleF ViewBox;

        protected GridLogic GridLogic;

        protected ItemCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size)
            : base(surface, block, size)
        {
            // Calcula a área realmente visível do painel (útil para alinhamentos).
            var sizeOffset = (surface.TextureSize - surface.SurfaceSize) / 2f;
            ViewBox = new RectangleF(sizeOffset.X, sizeOffset.Y, surface.SurfaceSize.X, surface.SurfaceSize.Y);
        }

        /// <summary>
        /// Fonte de dados do item (ex.: GridLogic.Ingots / Ores / Seeds / etc.)
        /// </summary>
        public abstract Dictionary<string, double> ItemSource { get; }

        public override void Run()
        {
            if (GridLogic == null && Block != null && Block.CubeGrid != null)
            {
                GridLogicSession.components.TryGetValue(Block.CubeGrid.EntityId, out GridLogic);
            }

            base.Run();
        }

        // =================== Helpers compartilhados ===================

        // Regex para filtros no nome do LCD
        protected static readonly Regex RxGroup     = new Regex(@"\(\s*G\s*:\s*(.+?)\s*\)", RegexOptions.IgnoreCase);
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
            catch { return 0; }
        }

        protected static int GetMaxRows(IMyTextSurface surf, float listStartY, float lineHeight)
        {
            float surfH = 512f;
            try { surfH = surf.SurfaceSize.Y; } catch { }
            float available = Math.Max(0f, surfH - listStartY - 10f);
            int rows = (int)Math.Floor(available / Math.Max(1f, lineHeight));
            return rows < 1 ? 1 : rows;
        }

        protected static void ParseFilter(IMyTerminalBlock lcd, out string mode, out string token)
        {
            mode = null; token = null;
            if (lcd == null) return;
            var name = lcd.CustomName ?? string.Empty;

            var mg = RxGroup.Match(name);
            if (mg.Success) { mode = "group"; token = mg.Groups[1].Value.Trim(); return; }

            var mc = RxContainer.Match(name);
            if (mc.Success) { mode = "container"; token = mc.Groups[1].Value.Trim(); }
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
    }
}
