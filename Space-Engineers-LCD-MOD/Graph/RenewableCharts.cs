using System;
using System.Collections.Generic;
using System.Globalization;

using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.Entities.Cube;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

using Graph.Data.Scripts.Graph.Panels;
using Sandbox.ModAPI;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("RenewableCharts", "Turbina & Painel Solar")]
    public class RenewableCharts : MyTextSurfaceScriptBase
    {
        private static readonly Vector2 TITLE_POS = new Vector2(16, 18);
        private static readonly Vector2 PIE_SOLAR  = new Vector2(90, 120);
        private static readonly Vector2 PIE_WIND   = new Vector2(90, 300);
        private static readonly Vector2 TEXT_POS   = new Vector2(210, 100);
        private const float LINE = 20f;

        private readonly PieChartPanel _pieSolar;
        private readonly PieChartPanel _pieWind;

        private static readonly CultureInfo Pt = new CultureInfo("pt-BR");

        public new IMyTextSurface Surface { get; set; }
        public new IMyCubeBlock Block { get; set; }
        public override ScriptUpdate NeedsUpdate { get { return ScriptUpdate.Update10; } }

        public RenewableCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface = surface;
            Block   = block;
            Surface.ContentType = ContentType.SCRIPT;

            _pieSolar = new PieChartPanel("", surface, new Vector2(PIE_SOLAR.X, 512 - PIE_SOLAR.Y), new Vector2(120), false);
            _pieWind  = new PieChartPanel("", surface, new Vector2(PIE_WIND.X,  512 - PIE_WIND.Y),  new Vector2(120), false);
        }

        public override void Run()
        {
            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                sprites.Add(Text("Turbina & Painel Solar", TITLE_POS, 0.95f));

                double curSolar=0, maxSolar=0, curWind=0, maxWind=0;
                SumRenewables(Block.CubeGrid, ref curSolar, ref maxSolar, ref curWind, ref maxWind);

                float useSolar = (float)((maxSolar > 0) ? Math.Min(Math.Max(curSolar / maxSolar, 0), 1) : 0);
                float useWind  = (float)((maxWind  > 0) ? Math.Min(Math.Max(curWind  / maxWind,  0), 1) : 0);

                sprites.AddRange(_pieSolar.GetSprites(useSolar, true));
                sprites.AddRange(_pieWind .GetSprites(useWind,  true));

                sprites.Add(Centered("Painel ( " + Pct(useSolar) + " )", PIE_SOLAR + new Vector2(0, 95), 0.8f));
                sprites.Add(Centered("Turbina ( " + Pct(useWind)  + " )", PIE_WIND  + new Vector2(0, 95), 0.8f));

                var p = TEXT_POS;
                sprites.Add(Text("Painéis Solares", p, 0.95f)); p += new Vector2(0, LINE);
                sprites.Add(Text("Gerando: " + Pow(curSolar) + " / " + Pow(maxSolar), p, 0.9f)); p += new Vector2(0, LINE);
                sprites.Add(Text("Uso: " + Pct(useSolar), p, 0.9f)); p += new Vector2(0, LINE * 2);

                sprites.Add(Text("Turbinas Eólicas", p, 0.95f)); p += new Vector2(0, LINE);
                sprites.Add(Text("Gerando: " + Pow(curWind) + " / " + Pow(maxWind), p, 0.9f)); p += new Vector2(0, LINE);
                sprites.Add(Text("Uso: " + Pct(useWind), p, 0.9f)); p += new Vector2(0, LINE);

                frame.AddRange(sprites);
            }
        }

        private void SumRenewables(IMyCubeGrid grid, ref double curSolar, ref double maxSolar, ref double curWind, ref double maxWind)
        {
            if (grid == null) return;

            var slims = new List<IMySlimBlock>();
            grid.GetBlocks(slims);

            for (int i = 0; i < slims.Count; i++)
            {
                var fat  = slims[i].FatBlock as IMyTerminalBlock;
                if (fat == null) continue;

                var prod = fat as Sandbox.ModAPI.IMyPowerProducer;
                if (prod == null) continue;

                string typeId = "";
                try { typeId = fat.BlockDefinition.TypeIdString ?? ""; } catch { }

                bool isSolar = typeId.EndsWith("SolarPanel", StringComparison.OrdinalIgnoreCase);
                bool isWind  = typeId.EndsWith("WindTurbine", StringComparison.OrdinalIgnoreCase);
                if (!isSolar && !isWind) continue;

                double cur = 0, max = 0;
                try { cur = prod.CurrentOutput; } catch { }
                try { max = prod.MaxOutput;     } catch { }

                if (isSolar) { curSolar += cur; maxSolar += max; }
                else         { curWind  += cur; maxWind  += max; }
            }
        }

        private MySprite Text(string s, Vector2 p, float scale)
        {
            return new MySprite { Type = SpriteType.TEXT, Data = s, Position = p,
                Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = scale };
        }
        private MySprite Centered(string s, Vector2 p, float scale)
        {
            return new MySprite { Type = SpriteType.TEXT, Data = s, Position = p,
                Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.CENTER, RotationOrScale = scale };
        }
        private string Pow(double mw)
        {
            double a = Math.Abs(mw);
            string sign = mw < 0 ? "-" : "";
            if (a >= 1000000.0) return sign + (a/1000000.0).ToString("0.##", Pt) + " MW";
            if (a >= 1.0)       return sign + a.ToString("0.##", Pt) + " MW";
            return sign + (a*1000.0).ToString("0.##", Pt) + " kW";
        }
        private string Pct(float f) { return ((int)Math.Round(f * 100f)).ToString(Pt) + "%"; }
    }
}
