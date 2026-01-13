using System;
using System.Collections.Generic;
using Graph.Helpers;
using Graph.Panels;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Graph.Charts
{
    [MyTextSurfaceScript("GasGraph", "Gas Graph")]
    public class GasGraph : ChartBase
    {
        private static readonly Vector2 INFO_POS = new Vector2(16f, 56f);

        private bool _first = true;
        private double _lastH2, _lastO2;
        private double _lastSec;

        private string _nameH2;
        private string _nameO2;
        
        

        public GasGraph(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface.ContentType = ContentType.SCRIPT;
            SetLocalizedTitleFromGame();
        }

        public override Dictionary<MyItemType, double> ItemSource => null;

        protected override string DefaultTitle => _localizedTitle;
        private string _localizedTitle;

        public override void Run()
        {
            base.Run();
            if (Config == null) return;

            if (_nameH2 == null) SetLocalizedTitleFromGame();

            var Scale = GetAutoScaleUniform();

            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();
                DrawTitle(sprites);

                string mode, token;
                ParseFilter(Block as IMyTerminalBlock, out mode, out token);

                double capH = 0, amtH = 0, capO = 0, amtO = 0;
                SumFluids((IMyCubeGrid)Block.CubeGrid, token, ref capH, ref amtH, ref capO, ref amtO);

                double sec = 0;
                try
                {
                    sec = MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds;
                }
                catch
                {
                }

                double inH = 0, outH = 0, inO = 0, outO = 0;
                if (!_first && sec > _lastSec)
                {
                    var dt = Math.Max(0.001, sec - _lastSec);
                    var rH = (amtH - _lastH2) / dt;
                    var rO = (amtO - _lastO2) / dt;

                    if (rH >= 0) inH = rH;
                    else outH = -rH;

                    if (rO >= 0) inO = rO;
                    else outO = -rO;
                }

                _lastH2 = amtH;
                _lastO2 = amtO;
                _lastSec = sec;
                _first = false;

                var padX = Clamp(ViewBox.Width * 0.06f * Scale, 10f * Scale, 32f * Scale);
                var topBase = INFO_POS.Y * Scale;
                var padBottom = Clamp(ViewBox.Height * 0.06f * Scale, 10f * Scale, 28f * Scale);

                var contentLeft = ViewBox.Position.X + padX;
                var contentTop = ViewBox.Position.Y + topBase;
                var contentWidth = Math.Max(40f * Scale, (ViewBox.Width * Scale) - (padX * 2f));
                var contentHeight = Math.Max(40f * Scale, (ViewBox.Height * Scale) - topBase - padBottom);

                var gapBetween = Clamp(contentHeight * 0.06f, 10f * Scale, 28f * Scale);

                var slotHeight = (contentHeight - gapBetween) * 0.5f;
                if (slotHeight < 10f * Scale) slotHeight = 10f * Scale;

                var nameScale = Clamp(slotHeight / (130f * Scale), 0.70f, 1.15f);
                var textScale = Clamp(slotHeight / (160f * Scale), 0.65f, 1.05f);

                var barW = Clamp(contentWidth * 0.70f, 40f * Scale, contentWidth);
                var barH = Clamp(slotHeight * 0.10f, 6f * Scale, 14f * Scale);

                var titleGap = Clamp(slotHeight * 0.18f, 18f * Scale, 42f * Scale); 
                var lineGap = Clamp(slotHeight * 0.10f, 10f * Scale, 26f * Scale);  
                var afterBar = Clamp(slotHeight * 0.08f, 8f * Scale, 20f * Scale);  

                var bg = Config.HeaderColor;
                var fg = Surface.ScriptForegroundColor;

                var p1 = new Vector2(contentLeft, contentTop);
                DrawGasBlock(
                    sprites: sprites,
                    pTopLeft: p1,
                    name: _nameH2,
                    cap: capH,
                    amt: amtH,
                    inRate: inH,
                    outRate: outH,
                    nameScale: nameScale * Scale,
                    textScale: textScale * Scale,
                    barW: barW,
                    barH: barH,
                    titleGap: titleGap,
                    lineGap: lineGap,
                    afterBar: afterBar,
                    fg: fg,
                    bg: bg
                );

                var p2 = new Vector2(contentLeft, contentTop + slotHeight + gapBetween);
                DrawGasBlock(
                    sprites: sprites,
                    pTopLeft: p2,
                    name: _nameO2,
                    cap: capO,
                    amt: amtO,
                    inRate: inO,
                    outRate: outO,
                    nameScale: nameScale * Scale,
                    textScale: textScale * Scale,
                    barW: barW,
                    barH: barH,
                    titleGap: titleGap,
                    lineGap: lineGap,
                    afterBar: afterBar,
                    fg: fg,
                    bg: bg
                );


                frame.AddRange(sprites);
            }
        }

        private void DrawGasBlock(
            List<MySprite> sprites,
            Vector2 pTopLeft,
            string name,
            double cap,
            double amt,
            double inRate,
            double outRate,
            float nameScale,
            float textScale,
            float barW,
            float barH,
            float titleGap,
            float lineGap,
            float afterBar,
            Color fg,
            Color bg)
        {
            var p = pTopLeft;

            sprites.Add(Text(name, p, 0.95f * nameScale));

            p += new Vector2(0, titleGap);

            var bar = new BarPanel(p, new Vector2(barW, barH), fg, bg);
            sprites.AddRange(bar.GetSprites(Fill(cap, amt), Config.HeaderColor));

            p += new Vector2(0, afterBar);
            sprites.Add(Text("Atual/Total: " + Gas(amt) + " / " + Gas(cap), p, 0.9f * textScale));

            p += new Vector2(0, lineGap);
            sprites.Add(Text("Entrada: " + GasRate(inRate) + "   Saída: " + GasRate(outRate), p, 0.9f * textScale));
        }

        private void SetLocalizedTitleFromGame()
        {
            _nameH2 = GetGasDisplayName("Hydrogen");
            _nameO2 = GetGasDisplayName("Oxygen");
            _localizedTitle = _nameH2 + " / " + _nameO2;
        }

        private string GetGasDisplayName(string subtype)
        {
            try
            {
                var id = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), subtype);

                MyGasProperties def;
                if (MyDefinitionManager.Static.TryGetDefinition(id, out def))
                {
                    var s = def.DisplayNameString;
                    if (!string.IsNullOrEmpty(s))
                        return s;

                    if (def.DisplayNameEnum.HasValue)
                    {
                        var sb = MyTexts.Get(def.DisplayNameEnum.Value);
                        if (sb != null)
                        {
                            s = sb.ToString();
                            if (!string.IsNullOrEmpty(s))
                                return s;
                        }
                    }

                    if (!string.IsNullOrEmpty(def.DisplayNameText))
                        return def.DisplayNameText;
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, GetType());
            }

            return subtype;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private float Fill(double cap, double amt)
        {
            if (cap <= 0) return 0f;
            var f = amt / cap;
            if (f < 0) f = 0;
            if (f > 1) f = 1;
            return (float)f;
        }

        private string Gas(double liters)
        {
            var a = Math.Abs(liters);
            var sign = liters < 0 ? "-" : "";
            if (a >= 1000000.0) return sign + (a / 1000000.0).ToString("0.##", Pt) + " ML";
            if (a >= 1000.0) return sign + (a / 1000.0).ToString("0.##", Pt) + " kL";
            return sign + a.ToString("0.#", Pt) + " L";
        }

        private string GasRate(double lps)
        {
            var a = Math.Abs(lps);
            var sign = lps < 0 ? "-" : "";
            if (a >= 1000000.0) return sign + (a / 1000000.0).ToString("0.##", Pt) + " ML/s";
            if (a >= 1000.0) return sign + (a / 1000.0).ToString("0.##", Pt) + " kL/s";
            return sign + a.ToString("0.#", Pt) + " L/s";
        }

        private void SumFluids(
            IMyCubeGrid grid, string token,
            ref double capH, ref double amtH,
            ref double capO, ref double amtO)
        {
            capH = capO = 0.0;
            amtH = amtO = 0.0;
            if (grid == null) return;

            var slims = new List<IMySlimBlock>();
            grid.GetBlocks(slims);

            for (var i = 0; i < slims.Count; i++)
            {
                var fat = slims[i].FatBlock as IMyTerminalBlock;
                if (fat == null) continue;

                if (!string.IsNullOrEmpty(token))
                {
                    var nm = fat.CustomName ?? "";
                    if (nm.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }

                var tank = fat as IMyGasTank;
                if (tank == null) continue;

                double cap = 0.0, ratio = 0.0;
                try
                {
                    cap = tank.Capacity;
                }
                catch (Exception e)
                {
                    ErrorHandlerHelper.LogError(e, GetType());
                }

                try
                {
                    ratio = tank.FilledRatio;
                }
                catch (Exception e)
                {
                    ErrorHandlerHelper.LogError(e, GetType());
                }

                var amt = cap * ratio;

                string gasSub = null;
                try
                {
                    var defBase = MyDefinitionManager.Static.GetCubeBlockDefinition(fat.BlockDefinition);
                    var gasDef = defBase as MyGasTankDefinition;
                    if (gasDef != null) gasSub = gasDef.StoredGasId.SubtypeName;
                }
                catch (Exception e)
                {
                    ErrorHandlerHelper.LogError(e, GetType());
                }

                if (!string.IsNullOrEmpty(gasSub))
                {
                    var s = gasSub.ToLowerInvariant();
                    if (s == "hydrogen")
                    {
                        capH += cap;
                        amtH += amt;
                        continue;
                    }

                    if (s == "oxygen")
                    {
                        capO += cap;
                        amtO += amt;
                        continue;
                    }
                }
                else
                {
                    var subTypeID = "";
                    try
                    {
                        subTypeID = fat.BlockDefinition.SubtypeName ?? "";
                    }
                    catch
                    {
                    }

                    if (subTypeID.IndexOf("Hydrogen", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        capH += cap;
                        amtH += amt;
                    }
                    else if (subTypeID.IndexOf("Oxygen", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        capO += cap;
                        amtO += amt;
                    }
                }
            }
        }
    }
}
