using System;
using System.Collections.Generic;
using System.Globalization;

using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.Entities.Cube;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("GasCharts", "Hidrogênio & Oxigênio")]
    public class GasCharts : MyTextSurfaceScriptBase
    {
        private static readonly Vector2 TITLE_POS = new Vector2(16, 20);
        private static readonly Vector2 INFO_POS  = new Vector2(16, 56);
        private const float LINE = 20f;

        private static readonly CultureInfo Pt = new CultureInfo("pt-BR");

        double _lastH2, _lastO2;
        double _lastSec;
        bool _first = true;

        public new IMyTextSurface Surface { get; set; }
        public new IMyCubeBlock Block { get; set; }
        public override ScriptUpdate NeedsUpdate { get { return ScriptUpdate.Update10; } }

        public GasCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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

                sprites.Add(Text("Hidrogênio & Oxigênio", TITLE_POS, 1.0f));

                double capH=0, amtH=0, capO=0, amtO=0;
                SumGas(Block.CubeGrid, ref capH, ref amtH, ref capO, ref amtO);

                double sec = 0;
                try { sec = MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds; } catch { }
                double inH=0, outH=0, inO=0, outO=0;
                if (!_first && sec > _lastSec)
                {
                    double dt = Math.Max(0.001, sec - _lastSec);
                    double rH = (amtH - _lastH2) / dt;
                    double rO = (amtO - _lastO2) / dt;
                    if (rH >= 0) inH = rH; else outH = -rH;
                    if (rO >= 0) inO = rO; else outO = -rO;
                }
                _lastH2 = amtH; _lastO2 = amtO; _lastSec = sec; _first = false;

                var p = INFO_POS;

                sprites.Add(Text("Hidrogênio", p, 0.95f)); p += new Vector2(0, LINE);
                DrawBar(ref sprites, p, Fill(capH, amtH)); p += new Vector2(0, LINE + 4);
                sprites.Add(Text("Atual/Total: " + Gas(amtH) + " / " + Gas(capH), p, 0.9f)); p += new Vector2(0, LINE);
                sprites.Add(Text("Entrada: " + GasRate(inH) + "   Saída: " + GasRate(outH), p, 0.9f)); p += new Vector2(0, LINE);

                p += new Vector2(0, LINE * 0.8f);

                sprites.Add(Text("Oxigênio", p, 0.95f)); p += new Vector2(0, LINE);
                DrawBar(ref sprites, p, Fill(capO, amtO)); p += new Vector2(0, LINE + 4);
                sprites.Add(Text("Atual/Total: " + Gas(amtO) + " / " + Gas(capO), p, 0.9f)); p += new Vector2(0, LINE);
                sprites.Add(Text("Entrada: " + GasRate(inO) + "   Saída: " + GasRate(outO), p, 0.9f)); p += new Vector2(0, LINE);

                frame.AddRange(sprites);
            }
        }

        private float Fill(double cap, double amt)
        {
            if (cap <= 0) return 0f;
            double f = amt / cap;
            if (f < 0) f = 0; if (f > 1) f = 1;
            return (float)f;
        }

        private void SumGas(IMyCubeGrid grid, ref double capH, ref double amtH, ref double capO, ref double amtO)
        {
            if (grid == null) return;

            var slims = new List<IMySlimBlock>();
            grid.GetBlocks(slims);

            for (int i = 0; i < slims.Count; i++)
            {
                var tank = slims[i].FatBlock as Sandbox.ModAPI.IMyGasTank;
                if (tank == null) continue;

                double cap = 0, ratio = 0;
                try { cap = tank.Capacity; } catch { }
                try { ratio = tank.FilledRatio; } catch { }
                double amt = cap * ratio;

                string sub = "";
                try { sub = tank.BlockDefinition.SubtypeName ?? ""; } catch { }

                bool isHydrogen = sub.IndexOf("hydrogen", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  sub.IndexOf("H2",       StringComparison.OrdinalIgnoreCase) >= 0;
                if (isHydrogen) { capH += cap; amtH += amt; }
                else            { capO += cap; amtO += amt; }
            }
        }

        private void DrawBar(ref List<MySprite> list, Vector2 pos, float frac)
        {
            float w = 320f, h = 10f;
            var bg = new MySprite { Type = SpriteType.TEXTURE, Data = "SquareSimple", Position = pos, Size = new Vector2(w, h),
                Color = new Color(50, 50, 50, 200), Alignment = TextAlignment.LEFT };
            list.Add(bg);

            var fg = new MySprite { Type = SpriteType.TEXTURE, Data = "SquareSimple", Position = pos, Size = new Vector2(w * frac, h),
                Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT };
            list.Add(fg);
        }

                private MySprite Text(string s, Vector2 p, float scale)
                {
                    return new MySprite { Type = SpriteType.TEXT, Data = s, Position = p,
                        Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = scale };
                }

                private string Gas(double u)
                {
                    if (u >= 1000000.0) return (u/1000000.0).ToString("0.##", Pt) + " ML";
                    if (u >= 1000.0)     return (u/1000.0).ToString("0.##", Pt) + " kL";
                    return u.ToString("0.#", Pt) + " L";
                }
                private string GasRate(double ups)
                {
                    if (ups >= 1000000.0) return (ups/1000000.0).ToString("0.##", Pt) + " ML/s";
                    if (ups >= 1000.0)     return (ups/1000.0).ToString("0.##", Pt) + " kL/s";
                    return ups.ToString("0.#", Pt) + " L/s";
                }
            }
        }
        