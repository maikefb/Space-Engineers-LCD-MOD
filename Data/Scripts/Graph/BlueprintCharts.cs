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
    [MyTextSurfaceScript("BlueprintCharts", "Blueprint Charts")]
    public class BlueprintCharts : MyTextSurfaceScriptBase
    {
        private static readonly Vector2 TITLE_POS = new Vector2(16, 20);
        private static readonly Vector2 INFO_POS  = new Vector2(16, 50);
        private const float LINE = 18f;

        private const int SCROLL_SECONDS = 2;

        private static readonly Regex RxProjToken = new Regex(@"\(\s*(.+?)\s*\)", RegexOptions.IgnoreCase);
        private static readonly Regex RxTotal   = new Regex(@"^\s*TotalBlocks\s*=\s*(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static readonly Regex RxRemain  = new Regex(@"^\s*RemainingBlocks\s*=\s*(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static readonly Regex RxMissing = new Regex(@"^\s*Missing\s*=\s*([\p{L}0-9 _\.\-]+)\s*:\s*(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly CultureInfo Pt = new CultureInfo("pt-BR");

        private string _token;

        public new IMyTextSurface Surface { get; set; }
        public new IMyCubeBlock Block { get; set; }
        public override ScriptUpdate NeedsUpdate { get { return ScriptUpdate.Update10; } }

        public BlueprintCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface = surface;
            Block = block;
            Surface.ContentType = ContentType.SCRIPT;
        }

        public override void Run()
        {
            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                var lcd = Block as IMyTerminalBlock;
                _token = ParseToken(lcd);

                int total = 1, remaining = 0;
                var projector = FindProjector(Block.CubeGrid, _token);
                if (projector != null)
                {
                    try { total = projector.TotalBlocks; remaining = projector.RemainingBlocks; }
                    catch { total = 1; remaining = 0; }
                }
                else
                {
                    TryReadCountsFromCustomData(lcd, ref total, ref remaining);
                }

                var title = "Blueprints";
                if (!string.IsNullOrEmpty(_token)) title += "  ·  (" + _token + ")";
                sprites.Add(new MySprite { Type = SpriteType.TEXT, Data = title, Position = TITLE_POS, Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = 0.95f });

                float frac = (float)(Math.Max(total - remaining, 0)) / (float)Math.Max(total, 1);
                int pct = (int)Math.Round(frac * 100);
                var pos = INFO_POS;
                sprites.Add(new MySprite { Type = SpriteType.TEXT, Data = "Progresso: " + pct + "%  (" + (total - remaining) + "/" + total + " blocos)", Position = pos, Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = 0.85f });
                pos += new Vector2(0, LINE);
                pos += new Vector2(0, LINE);

                sprites.Add(new MySprite { Type = SpriteType.TEXT, Data = "Faltantes (BP)", Position = pos, Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = 0.85f });
                pos += new Vector2(0, LINE);
                pos += new Vector2(0, LINE);

                var missing = ReadMissingFromCustomData(lcd);

                if (missing.Count == 0)
                {
                    sprites.Add(new MySprite { Type = SpriteType.TEXT, Data = "- sem dados -", Position = pos, Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = 0.78f });
                }
                else
                {
                    int maxRows = GetMaxRowsFromSurface(pos.Y);
                    if (maxRows < 1) maxRows = 1;

                    int requiredRows = missing.Count;
                    bool shouldScroll = requiredRows > (int)Math.Floor(maxRows * 0.95);

                    int visible = maxRows;
                    int start = 0;

                    if (shouldScroll && missing.Count > visible)
                    {
                        int step = GetScrollStep(SCROLL_SECONDS);
                        start = step % missing.Count;
                    }

                    int showCount = Math.Min(visible, missing.Count);
                    for (int visIdx = 0; visIdx < showCount; visIdx++)
                    {
                        int realIdx = (start + visIdx) % missing.Count;

                        int row = visIdx;
                        var p = pos + new Vector2(0f, row * LINE);

                        string line = missing[realIdx].Key + ": " + missing[realIdx].Value.ToString("#,0", Pt);
                        sprites.Add(new MySprite { Type = SpriteType.TEXT, Data = line, Position = p, Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = 0.78f });
                    }
                }

                frame.AddRange(sprites);
            }
        }

        private int GetScrollStep(int secondsPerStep)
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

        private int GetMaxRowsFromSurface(float listStartY)
        {
            float surfH = 512f;
            try { surfH = Surface.SurfaceSize.Y; } catch { }
            float available = Math.Max(0f, surfH - listStartY - 10f);
            int rows = (int)Math.Floor(available / LINE);
            return rows < 1 ? 1 : rows;
        }

        private string ParseToken(IMyTerminalBlock lcd)
        {
            if (lcd == null) return null;
            var m = RxProjToken.Match(lcd.CustomName ?? "");
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }

        private IMyProjector FindProjector(IMyCubeGrid grid, string token)
        {
            if (grid == null) return null;

            var slims = new List<IMySlimBlock>();
            grid.GetBlocks(slims);

            for (int i = 0; i < slims.Count; i++)
            {
                var fat = slims[i].FatBlock as IMyTerminalBlock;
                if (fat == null) continue;

                var proj = fat as IMyProjector;
                if (proj == null) continue;

                if (!string.IsNullOrEmpty(token))
                {
                    var name = fat.CustomName ?? "";
                    if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }
                return proj;
            }
            return null;
        }

        private void TryReadCountsFromCustomData(IMyTerminalBlock lcd, ref int total, ref int remaining)
        {
            if (lcd == null) return;
            var data = lcd.CustomData ?? "";

            var mt = RxTotal.Match(data);
            int tmp;
            if (mt.Success && int.TryParse(mt.Groups[1].Value, out tmp)) total = Math.Max(1, tmp);

            var mr = RxRemain.Match(data);
            if (mr.Success && int.TryParse(mr.Groups[1].Value, out tmp)) remaining = Math.Max(0, tmp);
        }

        private List<KeyValuePair<string,int>> ReadMissingFromCustomData(IMyTerminalBlock lcd)
        {
            var list = new List<KeyValuePair<string,int>>();
            if (lcd == null) return list;

            var data = lcd.CustomData ?? "";
            var ms = RxMissing.Matches(data);
            for (int i = 0; i < ms.Count; i++)
            {
                var name = ms[i].Groups[1].Value.Trim();
                int q = 0;
                int.TryParse(ms[i].Groups[2].Value, out q);
                list.Add(new KeyValuePair<string,int>(name, q));
            }
            list.Sort((a,b) => b.Value.CompareTo(a.Value));
            return list;
        }
    }
}
