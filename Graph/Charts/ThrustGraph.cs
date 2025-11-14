using System;
using System.Collections.Generic;
using Graph.Panels;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace Graph.Charts
{
    [MyTextSurfaceScript(ID, TITLE)]
    public class ThrustGraph : ChartBase
    {
        public const string ID = "MotorForceGraph";
        public const string TITLE = "HelpScreen_JoystickThrust";
        
        private static readonly Vector2 PIE_POS  = new Vector2(250, 320);
        private static readonly Vector2 INFO_POS = new Vector2(90, 355);

        private const float LINE       = 35f;   
        private const float PIE_RADIUS = 240f;  
        private const float PCT_FONT   = 1.5f;  

        public override Dictionary<VRage.Game.ModAPI.Ingame.MyItemType, double> ItemSource => null;
        protected override string DefaultTitle => TITLE;

        PieChartPanel pie;
        
        public ThrustGraph(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface.ContentType = ContentType.SCRIPT;
            
            pie = new PieChartPanel(
                "", (IMyTextSurface)Surface,
                ToScreenMargin(ViewBox.Position + (PIE_POS * 1)),
                new Vector2(PIE_RADIUS * 1),
                false);
        }

        protected override void LayoutChanged()
        {
            base.LayoutChanged();
            pie.SetMargin(ToScreenMargin(ViewBox.Position + PIE_POS * Scale),new Vector2(PIE_RADIUS * Scale));
        }
        
        public override void Run()
        {
            base.Run();
            if (Config == null) return;

            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                DrawTitle(sprites);
                DrawFooter(sprites);

                double massKg, gMag; Vector3D upDir;
                GetMassAndUp((IMyCubeGrid)Block?.CubeGrid, out massKg, out gMag, out upDir);
                double availUpN = SumAvailableUpThrust((IMyCubeGrid)Block?.CubeGrid, upDir);
                double needN    = massKg * gMag;

                float useFrac = 0f;
                if (availUpN > 0)
                    useFrac = (float)Math.Max(0.0, Math.Min(1.0, needN / availUpN));

                var pieCenterAbs = ViewBox.Position + (PIE_POS * Scale);
                float surfH = 512f; try { surfH = Surface.SurfaceSize.Y; } catch { }

                sprites.AddRange(pie.GetSprites(useFrac, Config.HeaderColor,true));

                sprites.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = ((int)Math.Round(useFrac * 100.0)).ToString() + "%",
                    Position = pieCenterAbs,
                    Color = Surface.ScriptForegroundColor,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = PCT_FONT * Scale
                });

                var p = ViewBox.Position + (INFO_POS * Scale);
                float lh = LINE * Scale;

                sprites.Add(Text("Necessário: " + PowForce(needN) , p, 1.5f * Scale)); p += new Vector2(0, lh);
                sprites.Add(Text("Disponível: " + PowForce(availUpN), p, 1.5f * Scale)); p += new Vector2(0, lh);
                p += new Vector2(0, lh); p += new Vector2(0, lh);

                if (availUpN <= 0.0)
                    sprites.Add(Warn("ATENÇÃO: sem empuxo disponível!", p));
                else if (needN > availUpN)
                    sprites.Add(Warn("ATENÇÃO: empuxo INSUFICIENTE (vai perder altitude)!", p));
                else if (useFrac >= 0.85f)
                    sprites.Add(Warn("Atenção: empuxo alto (≥85%) — margem pequena.", p));

                frame.AddRange(sprites);
            }
        }


        private MySprite Warn(string s, Vector2 pos)
        {
            return new MySprite
            {
                Type = SpriteType.TEXT,
                Data = s,
                Position = pos,
                Color = new Color(255, 80, 80),
                Alignment = TextAlignment.LEFT,
                RotationOrScale = 0.95f
            };
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
    }
}
