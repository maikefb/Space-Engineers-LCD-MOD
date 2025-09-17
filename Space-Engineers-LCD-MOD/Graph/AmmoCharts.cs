using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("AmmoCharts", "Munição")]
    public class AmmoCharts : MyTextSurfaceScriptBase
    {
        private static readonly Vector2 TITLE_POS = new Vector2(16, 20);
        private static readonly Vector2 INFO_POS  = new Vector2(16, 50);
        private const float LINE = 18f;
        private const int SCROLL_SECONDS = 2;

        private static readonly Regex RxGroup     = new Regex(@"\(\s*G\s*:\s*(.+?)\s*\)", RegexOptions.IgnoreCase);
        private static readonly Regex RxContainer = new Regex(@"\(\s*(?!G\s*:)(.+?)\s*\)", RegexOptions.IgnoreCase);

        private static readonly Regex RxSection = new Regex(@"\[AmmoCharts\](.*?)(?:\r?\n\r?\n|\Z)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex RxItem    = new Regex(@"^\s*Item\s*=\s*([\p{L}0-9 _\.\-]+)\s*:\s*([+-]?\d+(?:[.,]\d+)?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly CultureInfo Pt = new CultureInfo("pt-BR");

        public new IMyTextSurface Surface { get; set; }
        public new IMyCubeBlock Block { get; set; }
        public override ScriptUpdate NeedsUpdate { get { return ScriptUpdate.Update10; } }

        public AmmoCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface = surface; Block = block; Surface.ContentType = ContentType.SCRIPT;
        }

        public override void Run()
        {
            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                string mode, token; ParseFilter(Block as IMyTerminalBlock, out mode, out token);

                var title = "Munição";
                if (!string.IsNullOrEmpty(token))
                    title += (mode == "group") ? ("  ·  (G: " + token + ")") : ("  ·  (" + token + ")");
                sprites.Add(Text(title, TITLE_POS, 0.95f));

                var items = ReadItems(Block as IMyTerminalBlock);

                var pos = INFO_POS;
                sprites.Add(Text("Estoque de munições", pos, 0.85f));
                pos += new Vector2(0, LINE);
                pos += new Vector2(0, LINE);

                if (items.Count == 0)
                {
                    sprites.Add(Text("- sem dados -", pos, 0.78f));
                }
                else
                {
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
                        var rowPos = pos + new Vector2(0f, visIdx * LINE);

                        string line = items[realIdx].Key + ": " + FormatQty(items[realIdx].Value);
                        sprites.Add(Text(line, rowPos, 0.78f));
                    }
                }

                frame.AddRange(sprites);
            }
        }

        private MySprite Text(string s, Vector2 p, float scale)
        {
            return new MySprite { Type = SpriteType.TEXT, Data = s, Position = p, Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = scale };
        }

        private int GetScrollStep(int secondsPerStep)
        {
            try { var sess = MyAPIGateway.Session; if (sess == null) return 0; if (secondsPerStep <= 0) secondsPerStep = 1; double sec = sess.ElapsedPlayTime.TotalSeconds; return (int)(sec / secondsPerStep); }
            catch { return 0; }
        }

        private int GetMaxRowsFromSurface(float listStartY)
        {
            float surfH = 512f; try { surfH = Surface.SurfaceSize.Y; } catch { }
            float available = Math.Max(0f, surfH - listStartY - 10f);
            int rows = (int)Math.Floor(available / LINE);
            return rows < 1 ? 1 : rows;
        }

        private void ParseFilter(IMyTerminalBlock lcd, out string mode, out string token)
        {
            mode = null; token = null; if (lcd == null) return;
            var name = lcd.CustomName ?? string.Empty;
            var mg = RxGroup.Match(name); if (mg.Success) { mode = "group"; token = mg.Groups[1].Value.Trim(); return; }
            var mc = RxContainer.Match(name); if (mc.Success) { mode = "container"; token = mc.Groups[1].Value.Trim(); return; }
        }

        private List<KeyValuePair<string,double>> ReadItems(IMyTerminalBlock lcd)
        {
            var list = new List<KeyValuePair<string,double>>(); if (lcd == null) return list;
            var data = lcd.CustomData ?? "";
            var sec = RxSection.Match(data); if (!sec.Success) return list;

            var ms = RxItem.Matches(sec.Groups[1].Value);
            for (int i = 0; i < ms.Count; i++)
            {
                var key = ms[i].Groups[1].Value.Trim();
                var valS = ms[i].Groups[2].Value.Replace(',', '.');

                double q = 0.0;
                if (double.TryParse(valS, NumberStyles.Float, CultureInfo.InvariantCulture, out q))
                    list.Add(new KeyValuePair<string,double>(key, q));
            }

            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            return list;
        }

        private string FormatQty(double v)
        {
            if (v >= 1000) return Math.Round(v).ToString("#,0", new CultureInfo("pt-BR"));
            return v.ToString("0.##", new CultureInfo("pt-BR"));
        }
    }
}
