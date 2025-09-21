using System;
using System.Collections.Generic;
using Graph.Data.Scripts.Graph.Panels;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Helpers;
using VRage;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("GasGraph", "Gas Graph")]
    public class GasGraph : ChartBase
    {
        private const float LINE = 35f;
        private static readonly Vector2 INFO_POS = new Vector2(16f, 56f);

        private bool _first = true;
        private double _lastH2, _lastO2, _lastW;
        private double _lastSec;

        private string _nameH2;
        private string _nameO2;
        private string _nameW;

        public GasGraph(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface.ContentType = ContentType.SCRIPT;
            SetLocalizedTitleFromGame();
        }

        public override Dictionary<MyItemType, double> ItemSource => null;

        protected override string DefaultTitle { get; set; }

        public override void Run()
        {
            base.Run();
            if (Config == null) return;

            if (_nameH2 == null) SetLocalizedTitleFromGame();

            var scale = GetAutoScaleUniform();

            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();
                DrawTitle(sprites);

                string mode, token;
                ParseFilter(Block as IMyTerminalBlock, out mode, out token);

                double capH = 0, amtH = 0, capO = 0, amtO = 0, capW = 0, amtW = 0;
                SumFluids((IMyCubeGrid)Block.CubeGrid, token, ref capH, ref amtH, ref capO, ref amtO, ref capW,
                    ref amtW);

                double sec = 0;
                try
                {
                    sec = MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds;
                }
                catch
                {
                }

                double inH = 0, outH = 0, inO = 0, outO = 0, inW = 0, outW = 0;
                if (!_first && sec > _lastSec)
                {
                    var dt = Math.Max(0.001, sec - _lastSec);
                    var rH = (amtH - _lastH2) / dt;
                    var rO = (amtO - _lastO2) / dt;
                    var rW = (amtW - _lastW) / dt;
                    if (rH >= 0) inH = rH;
                    else outH = -rH;
                    if (rO >= 0) inO = rO;
                    else outO = -rO;
                    if (rW >= 0) inW = rW;
                    else outW = -rW;
                }

                _lastH2 = amtH;
                _lastO2 = amtO;
                _lastW = amtW;
                _lastSec = sec;
                _first = false;

                var p = ViewBox.Position + INFO_POS * scale;
                var lh = LINE * scale;

                var barW = Math.Max(40f, ViewBox.Width * 0.625f * scale);
                var barH = 10f * scale;

                var bg = new Color(50, 50, 50, 200);
                var fg = Surface.ScriptForegroundColor;

                sprites.Add(Text(_nameH2, p, 0.95f * scale));
                p += new Vector2(0, lh);
                var hBar = new BarPanel(p, new Vector2(barW, barH), fg, bg);
                sprites.AddRange(hBar.GetSprites(Fill(capH, amtH), Config.HeaderColor));
                p += new Vector2(0, 15f * scale);
                sprites.Add(Text("Atual/Total: " + Gas(amtH) + " / " + Gas(capH), p, 0.9f * scale));
                p += new Vector2(0, lh);
                sprites.Add(Text("Entrada: " + GasRate(inH) + "   Saída: " + GasRate(outH), p, 0.9f * scale));
                p += new Vector2(0, lh * 1.5f);

                sprites.Add(Text(_nameO2, p, 0.95f * scale));
                p += new Vector2(0, lh);
                var oBar = new BarPanel(p, new Vector2(barW, barH), fg, bg);
                sprites.AddRange(oBar.GetSprites(Fill(capO, amtO), Config.HeaderColor));
                p += new Vector2(0, 15f * scale);
                sprites.Add(Text("Atual/Total: " + Gas(amtO) + " / " + Gas(capO), p, 0.9f * scale));
                p += new Vector2(0, lh);
                sprites.Add(Text("Entrada: " + GasRate(inO) + "   Saída: " + GasRate(outO), p, 0.9f * scale));
                p += new Vector2(0, lh * 1.5f);

                sprites.Add(Text(_nameW, p, 0.95f * scale));
                p += new Vector2(0, lh);
                var wBar = new BarPanel(p, new Vector2(barW, barH), fg, bg);
                sprites.AddRange(wBar.GetSprites(Fill(capW, amtW), Config.HeaderColor));
                p += new Vector2(0, 15f * scale);
                sprites.Add(Text("Atual/Total: " + Gas(amtW) + " / " + Gas(capW), p, 0.9f * scale));
                p += new Vector2(0, lh);
                sprites.Add(Text("Entrada: " + GasRate(inW) + "   Saída: " + GasRate(outW), p, 0.9f * scale));

                frame.AddRange(sprites);
            }
        }

        private void SetLocalizedTitleFromGame()
        {
            _nameH2 = GetGasDisplayName("Hydrogen");
            _nameO2 = GetGasDisplayName("Oxygen");
            _nameW = GetGasDisplayName("Water");

            var localizedTitle = _nameH2 + " / " + _nameO2 + " / " + _nameW;
            DefaultTitle = localizedTitle;
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
            ref double capO, ref double amtO,
            ref double capW, ref double amtW)
        {
            capH = capO = capW = 0.0;
            amtH = amtO = amtW = 0.0;
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

                    if (s == "water")
                    {
                        capW += cap;
                        amtW += amt;
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
                    else if (subTypeID.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        capW += cap;
                        amtW += amt;
                    }
                }
            }
        }
    }
}