using System;
using System.Collections.Generic;

using Sandbox.Game.GameSystems.TextSurfaceScripts;
using VRage.Game.GUI.TextPanel;
using VRageMath;

using Graph.Data.Scripts.Graph.Panels;
using Sandbox.ModAPI;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using IMyCubeGrid = VRage.Game.ModAPI.IMyCubeGrid;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("RenewableCharts", "Turbina & Painel Solar")]
    public class RenewableCharts : ChartBase
    {
        private static readonly Vector2 TITLE_POS = new Vector2(16, 18);
        private static readonly Vector2 PIE_SOLAR  = new Vector2(90, 240);
        private static readonly Vector2 PIE_WIND   = new Vector2(90, 440);
        private static readonly Vector2 TEXT_POS_SOLAR   = new Vector2(180, 140);
        private static readonly Vector2 TEXT_POS_WIND   = new Vector2(180, 340);
        private const float LINE = 25f;

        public override Dictionary<MyItemType, double> ItemSource => null;
        public override string Title { get; protected set; } = "Turbina & Painel Solar";
        public RenewableCharts(IMyTextSurface surface, VRage.Game.ModAPI.IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface.ContentType = ContentType.SCRIPT;
        }

        public override void Run()
        {
            base.Run();

            if (Config == null)
                return;
            
            if (Math.Abs(CurrentTextPadding - Surface.TextPadding) > 0.1f)
                UpdateViewBox();
            
            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                DrawTitle(sprites, 1f);
                
                double curSolar=0, maxSolar=0, curWind=0, maxWind=0;
                SumRenewables((IMyCubeGrid)Block?.CubeGrid, ref curSolar, ref maxSolar, ref curWind, ref maxWind);

                float useSolar = (float)((maxSolar > 0) ? Math.Min(Math.Max(curSolar / maxSolar, 0), 1) : 0);
                float useWind  = (float)((maxWind  > 0) ? Math.Min(Math.Max(curWind  / maxWind,  0), 1) : 0);
                
                var pieSolar = new PieChartPanel(
                    "", (IMyTextSurface)Surface,
                    ToScreenMargin(ViewBox.Position + PIE_SOLAR),
                    new Vector2(120), false, Config.HeaderColor);

                var pieWind = new PieChartPanel(
                    "", (IMyTextSurface)Surface,
                    ToScreenMargin(ViewBox.Position + PIE_WIND),
                    new Vector2(120), false, Config.HeaderColor);

                sprites.AddRange(pieSolar.GetSprites(useSolar, true));
                sprites.AddRange(pieWind .GetSprites(useWind,  true));

                sprites.Add(Centered("Painel ( " + Pct(useSolar) + " )", ViewBox.Position + PIE_SOLAR - new Vector2(-15, 160), 0.8f));
                sprites.Add(Centered("Turbina ( " + Pct(useWind)  + " )", ViewBox.Position + PIE_WIND  - new Vector2(-15, 160), 0.8f));

                var solarVector = ViewBox.Position + TEXT_POS_SOLAR;
                sprites.Add(Text("Painéis Solares", solarVector, 0.95f)); solarVector += new Vector2(0, LINE);
                sprites.Add(Text("Geração Max: " + Pow(maxSolar), solarVector, 0.9f)); solarVector += new Vector2(0, LINE);
                sprites.Add(Text("Gerando: " + Pow(curSolar), solarVector, 0.9f)); solarVector += new Vector2(0, LINE);
                sprites.Add(Text("Uso: " + Pct(useSolar), solarVector, 0.9f)); solarVector += new Vector2(0, LINE * 2);

                var windVector = ViewBox.Position + TEXT_POS_WIND;
                sprites.Add(Text("Turbinas Eólicas", windVector, 0.95f)); windVector += new Vector2(0, LINE);
                sprites.Add(Text("Geração Max: " + Pow(maxWind), windVector, 0.9f)); windVector += new Vector2(0, LINE);
                sprites.Add(Text("Gerando: " + Pow(curWind), windVector, 0.9f)); windVector += new Vector2(0, LINE);
                sprites.Add(Text("Uso: " + Pct(useWind), windVector, 0.9f)); windVector += new Vector2(0, LINE);

                frame.AddRange(sprites);
            }
        }

        private void SumRenewables(VRage.Game.ModAPI.IMyCubeGrid grid, ref double curSolar, ref double maxSolar, ref double curWind, ref double maxWind)
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
        
    }
}
