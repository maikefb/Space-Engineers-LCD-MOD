using System;
using System.Collections.Generic;
using System.Globalization;

using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

using Graph.Data.Scripts.Graph.Panels;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("MotorForceCharts", "Força dos Motores (solo)")]
    public class MotorForceCharts : MyTextSurfaceScriptBase
    {
        private static readonly Vector2 TITLE_POS   = new Vector2(16, 18);
        private static readonly Vector2 PIE_POS     = new Vector2(256, 140);
        private static readonly Vector2 INFO_POS    = new Vector2(16, 230);
        private const float LINE = 20f;

        private readonly PieChartPanel _pie;
        private static readonly CultureInfo Pt = new CultureInfo("pt-BR");

        public new IMyTextSurface Surface { get; set; }
        public new IMyCubeBlock Block { get; set; }
        public override ScriptUpdate NeedsUpdate { get { return ScriptUpdate.Update10; } }

        public MotorForceCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface = surface;
            Block   = block;
            Surface.ContentType = ContentType.SCRIPT;
            _pie = new PieChartPanel("", surface, new Vector2(PIE_POS.X, 512f - PIE_POS.Y), new Vector2(130f), false);
        }

        public override void Run()
        {
            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                double massKg, gMag; Vector3D upDir;
                GetMassAndUp(Block.CubeGrid, out massKg, out gMag, out upDir);
                double availUpN = SumAvailableUpThrust(Block.CubeGrid, upDir);
                double needN    = massKg * gMag;

                float useFrac = 0f;
                if (availUpN > 0) useFrac = (float)Math.Max(0.0, Math.Min(1.0, needN / availUpN));

                sprites.Add(Text("Força dos Motores (solo)", TITLE_POS, 0.95f));
                sprites.AddRange(_pie.GetSprites(useFrac, true));
                sprites.Add(new MySprite{ Type = SpriteType.TEXT, Data = ((int)Math.Round(useFrac * 100.0)).ToString() + "%", Position = PIE_POS, Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.CENTER, RotationOrScale = 1.2f });

                var p = INFO_POS;
                sprites.Add(Text("Necessário: " + NkN(needN) + "   ·   Disponível: " + NkN(availUpN) + "   ·   g: " + gMag.ToString("0.00", Pt) + " m/s²", p, 0.9f));
                p += new Vector2(0, LINE);

                if (availUpN <= 0.0)
                    sprites.Add(Warn("ATENÇÃO: sem empuxo disponível!"));
                else if (needN > availUpN)
                    sprites.Add(Warn("ATENÇÃO: empuxo INSUFICIENTE (vai perder altitude)!"));
                else if (useFrac >= 0.85f)
                    sprites.Add(Warn("Atenção: empuxo alto (≥85%) — margem pequena."));

                frame.AddRange(sprites);
            }
        }

        private void GetMassAndUp(IMyCubeGrid grid, out double massKg, out double gMag, out Vector3D upUnit)
        {
            massKg = 0; gMag = 0; upUnit = Vector3D.Up;
            if (grid == null) return;

            var slims = new List<IMySlimBlock>();
            grid.GetBlocks(slims);

            Sandbox.ModAPI.IMyShipController ctrl = null;
            for (int i = 0; i < slims.Count; i++)
            {
                var fat = slims[i].FatBlock as Sandbox.ModAPI.IMyShipController;
                if (fat != null) { ctrl = fat; break; }
            }

            if (ctrl != null)
            {
                try { massKg = ctrl.CalculateShipMass().TotalMass; } catch { }
                Vector3D g = Vector3D.Zero;
                try { g = ctrl.GetNaturalGravity(); } catch { }
                if (g.LengthSquared() < 1e-6) { try { g = ctrl.GetArtificialGravity(); } catch { } }
                if (g.LengthSquared() > 1e-6) { gMag = g.Length(); upUnit = -Vector3D.Normalize(g); }
            }

            if (massKg <= 0) { try { massKg = grid.Physics.Mass; } catch { } }
            if (gMag <= 0)   { upUnit = grid.WorldMatrix.Up; gMag = 0; }
        }

        private double SumAvailableUpThrust(IMyCubeGrid grid, Vector3D upUnit)
        {
            if (grid == null) return 0.0;

            var slims = new List<IMySlimBlock>();
            grid.GetBlocks(slims);

            double sumN = 0.0;
            for (int i = 0; i < slims.Count; i++)
            {
                var thr = slims[i].FatBlock as Sandbox.ModAPI.IMyThrust;
                if (thr == null) continue;

                Vector3D thrustDir = -thr.WorldMatrix.Forward;
                double align = Vector3D.Dot(Vector3D.Normalize(thrustDir), upUnit);
                if (align <= 0) continue;

                double max = 0.0;
                try { max = thr.MaxEffectiveThrust; } catch { try { max = thr.MaxThrust; } catch { } }
                if (max > 0) sumN += max * align;
            }
            return sumN;
        }

        private MySprite Text(string s, Vector2 p, float scale)
        {
            return new MySprite { Type = SpriteType.TEXT, Data = s, Position = p,
                Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, RotationOrScale = scale };
        }
        private MySprite Warn(string s)
        {
            return new MySprite { Type = SpriteType.TEXT, Data = s, Position = INFO_POS + new Vector2(0, LINE),
                Color = new Color(255, 80, 80), Alignment = TextAlignment.LEFT, RotationOrScale = 0.95f };
        }
        private string NkN(double newtons)
        {
            double a = Math.Abs(newtons); string sign = newtons < 0 ? "-" : "";
            if (a >= 1000000.0) return sign + (a / 1000000.0).ToString("0.##", Pt) + " MN";
            if (a >= 1000.0)     return sign + (a / 1000.0).ToString("0.##", Pt)    + " kN";
            return sign + a.ToString("0.##", Pt) + " N";
        }
    }
}
