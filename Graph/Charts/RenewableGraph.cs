using System;
using System.Collections.Generic;
using Graph.Helpers;
using Graph.Panels;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using IMyCubeGrid = VRage.Game.ModAPI.IMyCubeGrid;

namespace Graph.Charts
{
    [MyTextSurfaceScript("RenewableGraph", "Turbina & Painel Solar")]
    public class RenewableGraph : ChartBase
    {
        private const float LINE = 25f;
        private static readonly Vector2 TITLE_POS = new Vector2(16, 18);
        private static readonly Vector2 PIE_SOLAR = new Vector2(90, 240);
        private static readonly Vector2 PIE_WIND = new Vector2(90, 440);
        private static readonly Vector2 TEXT_POS_SOLAR = new Vector2(180, 140);
        private static readonly Vector2 TEXT_POS_WIND = new Vector2(180, 340);

        private readonly PieChartPanel pieSolar;
        private readonly PieChartPanel pieWind;

        public RenewableGraph(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface.ContentType = ContentType.SCRIPT;

            pieSolar = new PieChartPanel(
                "",
                (IMyTextSurface)Surface,
                ToScreenMargin(ViewBox.Position + PIE_SOLAR),
                new Vector2(120f),
                false
            );


            pieWind = new PieChartPanel(
                "",
                (IMyTextSurface)Surface,
                ToScreenMargin(ViewBox.Position + PIE_WIND),
                new Vector2(120f),
                false
            );
        }

        public override Dictionary<MyItemType, double> ItemSource => null;
        protected override string DefaultTitle => "Turbina & Painel Solar";

        protected override void LayoutChanged()
        {
            base.LayoutChanged();

            pieSolar.SetMargin(ToScreenMargin(ViewBox.Position + PIE_SOLAR * Scale), new Vector2(120f * Scale));
            pieWind.SetMargin(ToScreenMargin(ViewBox.Position + PIE_WIND * Scale), new Vector2(120f * Scale));
        }

        public override void Run()
        {
            base.Run();

            if (Config == null)
                return;

            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                DrawTitle(sprites);
                DrawFooter(sprites);

                double curSolar = 0, maxSolar = 0, curWind = 0, maxWind = 0;
                SumRenewables((IMyCubeGrid)Block?.CubeGrid, ref curSolar, ref maxSolar, ref curWind, ref maxWind);

                var useSolar = (float)(maxSolar > 0 ? Math.Min(Math.Max(curSolar / maxSolar, 0), 1) : 0);
                var useWind = (float)(maxWind > 0 ? Math.Min(Math.Max(curWind / maxWind, 0), 1) : 0);

                sprites.AddRange(pieSolar.GetSprites(useSolar, Config.HeaderColor, true));
                sprites.AddRange(pieWind.GetSprites(useWind, Config.HeaderColor, true));

                sprites.Add(Centered("Painel ( " + Pct(useSolar) + " )",
                    ViewBox.Position + PIE_SOLAR * Scale - new Vector2(-15f * Scale, 160f * Scale),
                    0.8f * Scale));

                sprites.Add(Centered("Turbina ( " + Pct(useWind) + " )",
                    ViewBox.Position + PIE_WIND * Scale - new Vector2(-15f * Scale, 160f * Scale),
                    0.8f * Scale));

                var solarVector = ViewBox.Position + TEXT_POS_SOLAR * Scale;
                sprites.Add(Text("Painéis Solares", solarVector, 0.95f * Scale));
                solarVector += new Vector2(0, LINE * Scale);
                sprites.Add(Text("Geração Max: " + Pow(maxSolar), solarVector, 0.9f * Scale));
                solarVector += new Vector2(0, LINE * Scale);
                sprites.Add(Text("Gerando: " + Pow(curSolar), solarVector, 0.9f * Scale));
                solarVector += new Vector2(0, LINE * Scale);
                sprites.Add(Text("Uso: " + Pct(useSolar), solarVector, 0.9f * Scale));
                solarVector += new Vector2(0, LINE * 2f * Scale);

                var windVector = ViewBox.Position + TEXT_POS_WIND * Scale;
                sprites.Add(Text("Turbinas Eólicas", windVector, 0.95f * Scale));
                windVector += new Vector2(0, LINE * Scale);
                sprites.Add(Text("Geração Max: " + Pow(maxWind), windVector, 0.9f * Scale));
                windVector += new Vector2(0, LINE * Scale);
                sprites.Add(Text("Gerando: " + Pow(curWind), windVector, 0.9f * Scale));
                windVector += new Vector2(0, LINE * Scale);
                sprites.Add(Text("Uso: " + Pct(useWind), windVector, 0.9f * Scale));
                windVector += new Vector2(0, LINE * Scale);

                frame.AddRange(sprites);
            }
        }


        private void SumRenewables(
            IMyCubeGrid grid,
            ref double curSolar, ref double maxSolar,
            ref double curWind, ref double maxWind)
        {
            curSolar = maxSolar = curWind = maxWind = 0.0;
            if (grid == null) return;

            var producers = new List<IMyPowerProducer>();
            GridGroupsHelper
                .GetAllLogicBlocksOfType(
                    grid, producers, GridLinkTypeEnum.Logical);

            for (var i = 0; i < producers.Count; i++)
            {
                var prod = producers[i];

                var typeId = "";
                try
                {
                    typeId = prod.BlockDefinition.TypeIdString ?? "";
                }
                catch (Exception e)
                {
                    ErrorHandlerHelper.LogError(e, GetType());
                }

                var isSolar = typeId.EndsWith("SolarPanel", StringComparison.OrdinalIgnoreCase);
                var isWind = typeId.EndsWith("WindTurbine", StringComparison.OrdinalIgnoreCase);
                if (!isSolar && !isWind) continue;

                double cur = ToWatts(prod?.CurrentOutput ?? 0);
                double max = ToWatts(prod?.MaxOutput ?? 0);

                if (isSolar)
                {
                    curSolar += cur;
                    maxSolar += max;
                }
                else
                {
                    curWind += cur;
                    maxWind += max;
                }
            }
        }
        public double ToWatts(float powerUnit)
        {
            return powerUnit * 1000000;
        }
    }
}