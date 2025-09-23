using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Graph.Panels;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace Space_Engineers_LCD_MOD.Graph
{
    [MyTextSurfaceScript("ContainerCharts", "Contêineres")]
    public class ContainerCharts : MyTextSurfaceScriptBase
    {
        private static readonly Vector2 TITLE_POS = new Vector2(16, 20);
        private static readonly Vector2 PIE_POS   = new Vector2(110, 110); // pizza pequena canto sup/esq
        private static readonly Vector2 INFO_POS  = new Vector2(230, 60);  // texto à direita da pizza
        private const float LINE = 18f;

        private static readonly Regex RxGroup     = new Regex(@"\(\s*G\s*:\s*(.+?)\s*\)", RegexOptions.IgnoreCase);
        private static readonly Regex RxContainer = new Regex(@"\(\s*(?!G\s*:)(.+?)\s*\)", RegexOptions.IgnoreCase);

        private static readonly CultureInfo Pt = new CultureInfo("pt-BR");

        public new IMyTextSurface Surface { get; set; }
        public new IMyCubeBlock Block { get; set; }
        public override ScriptUpdate NeedsUpdate { get { return ScriptUpdate.Update10; } }

        private PieChartPanel _pie;

        public ContainerCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface = surface;
            Block = block;
            Surface.ContentType = ContentType.SCRIPT;

            var margin = new Vector2(PIE_POS.X, 512f - PIE_POS.Y);
            _pie = new PieChartPanel("Ocupação", surface, margin, new Vector2(120f), true);
        }

        public override void Run()
        {
            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                string mode, token;
                ParseFilter(Block as IMyTerminalBlock, out mode, out token);

                // Título
                var title = "Contêineres";
                if (!string.IsNullOrEmpty(token))
                    title += (mode == "group") ? ("  ·  (G: " + token + ")") : ("  ·  (" + token + ")");
                sprites.Add(Text(title, TITLE_POS, 0.95f));

                // Soma volumes dos contêineres
                double used = 0, cap = 0;
                int matched = 0;
                AggregateVolumes(Block.CubeGrid, token, ref used, ref cap, ref matched);

                float frac = 0f;
                if (cap > 1e-6) frac = (float)Math.Max(0.0, Math.Min(1.0, used / cap));

                // Pizza
                sprites.AddRange(_pie.GetSprites(frac, null ,true));

                // Texto
                var pos = INFO_POS;
                sprites.Add(Text("Resumo", pos, 0.88f));
                pos += new Vector2(0, LINE);

                if (matched <= 0)
                {
                    sprites.Add(Text("- nenhum contêiner encontrado -", pos, 0.80f));
                }
                else
                {
                    sprites.Add(Text("Blocos: " + matched, pos, 0.80f));
                    pos += new Vector2(0, LINE);

                    string sUsed = VolumeString(used);
                    string sCap  = VolumeString(cap);
                    int pct = (int)Math.Round(frac * 100);

                    sprites.Add(Text("Ocupação: " + pct + "%", pos, 0.85f));
                    pos += new Vector2(0, LINE);

                    sprites.Add(Text("Usado/Total: " + sUsed + " / " + sCap, pos, 0.80f));
                    pos += new Vector2(0, LINE);
                }

                frame.AddRange(sprites);
            }
        }

        private void ParseFilter(IMyTerminalBlock lcd, out string mode, out string token)
        {
            mode = null; token = null;
            if (lcd == null) return;

            var nm = lcd.CustomName ?? string.Empty;
            var mg = RxGroup.Match(nm);
            if (mg.Success) { mode = "group"; token = mg.Groups[1].Value.Trim(); return; }

            var mc = RxContainer.Match(nm);
            if (mc.Success) { mode = "container"; token = mc.Groups[1].Value.Trim(); }
        }

        private void AggregateVolumes(IMyCubeGrid grid, string token, ref double used, ref double cap, ref int matched)
        {
            used = 0; cap = 0; matched = 0;
            if (grid == null) return;

            var slims = new List<IMySlimBlock>();
            grid.GetBlocks(slims);

            for (int i = 0; i < slims.Count; i++)
            {
                var fat = slims[i].FatBlock as IMyTerminalBlock;
                if (fat == null) continue;

                if (!string.IsNullOrEmpty(token))
                {
                    var n = fat.CustomName ?? "";
                    if (n.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }

                string typeIdStr = "";
                try { typeIdStr = fat.BlockDefinition.TypeId.ToString(); } catch { }

                if (typeIdStr.IndexOf("CargoContainer", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (!fat.HasInventory) continue;

                double localUsed = 0, localCap = 0;
                int invCount = fat.InventoryCount;
                for (int k = 0; k < invCount; k++)
                {
                    var inv = fat.GetInventory(k);
                    if (inv == null) continue;

                    try
                    {
                        localUsed += (double)inv.CurrentVolume;
                        localCap  += (double)inv.MaxVolume;
                    }
                    catch { }
                }

                if (localCap > 0)
                {
                    matched++;
                    used += localUsed;
                    cap  += localCap;
                }
            }
        }

        private MySprite Text(string s, Vector2 p, float scale)
        {
            return new MySprite { Type = SpriteType.TEXT, Data = s, Position = p, Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = scale };
        }

        private string VolumeString(double v)
        {
            if (v >= 1000000) return (v / 1000000d).ToString("0.##", Pt) + "M";
            if (v >= 1000)     return (v / 1000d).ToString("0.##", Pt) + "k";
            return v.ToString("0.##", Pt);
        }
    }
}
