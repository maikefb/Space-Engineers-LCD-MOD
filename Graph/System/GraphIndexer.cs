using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Graph.Helpers;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using IngameItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;

namespace Graph.System
{
    // Todo: move all logic to Client side
    
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 100)]
    public class GraphIndexer : MySessionComponentBase
    {
        const int TICKS_INTERVAL = 120; // ~2s
        int _ticks;

        static readonly Regex RxGroup     = new Regex(@"\(\s*G\s*:\s*(.+?)\s*\)", RegexOptions.IgnoreCase);
        static readonly Regex RxContainer = new Regex(@"\(\s*(?!G\s*:)(.+?)\s*\)", RegexOptions.IgnoreCase);
        static readonly Regex RxProjToken = new Regex(@"\(\s*(.+?)\s*\)", RegexOptions.IgnoreCase);

        static readonly Regex RxMissA = new Regex(@"^\s*([\p{L}0-9][\p{L}0-9 _\.\-]+?)\s*[x×]\s*([0-9][0-9\.\, ]*)\s*$", RegexOptions.IgnoreCase);
        static readonly Regex RxMissB = new Regex(@"^\s*([0-9][0-9\.\, ]*)\s*[x×]\s*([\p{L}0-9][\p{L}0-9 _\.\-]+?)\s*$", RegexOptions.IgnoreCase);
        static readonly Regex RxMissC = new Regex(@"^\s*([\p{L}0-9][\p{L}0-9 _\.\-]+?)\s*:\s*([0-9][0-9\.\, ]*)\s*$", RegexOptions.IgnoreCase);

        class GasSnapshot
        {
            public double LastSec;
            public double LastH2;
            public double LastO2;
        }
        static readonly Dictionary<string, GasSnapshot> _gasSnaps = new Dictionary<string, GasSnapshot>(StringComparer.OrdinalIgnoreCase);

        class TankSnap
        {
            public double LastSec;
            public double LastAmount;
        }
        static readonly Dictionary<long, TankSnap> _tankSnaps = new Dictionary<long, TankSnap>();

        class TankInfo
        {
            public IMyTerminalBlock Block;
            public string GasKind; 
            public string TypeName;
            public double Capacity;
            public double Amount;
            public double Rate;
        }

        public override void UpdateBeforeSimulation()
        {
            if (MyAPIGateway.Multiplayer != null && !MyAPIGateway.Multiplayer.IsServer) return;

            _ticks++;
            if (_ticks % TICKS_INTERVAL != 0) return;

            try
            {
                var ents = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(ents, e => e is IMyCubeGrid);

                foreach (var ent in ents)
                {
                    var grid = ent as IMyCubeGrid;
                    if (grid == null) continue;

                    var slims = new List<IMySlimBlock>();
                    grid.GetBlocks(slims);

                    var lcds       = new List<Sandbox.ModAPI.IMyTextPanel>();
                    var invBlocks  = new List<IMyTerminalBlock>();
                    var projectors = new List<Sandbox.ModAPI.IMyProjector>();

                    for (int s = 0; s < slims.Count; s++)
                    {
                        var fat = slims[s].FatBlock as IMyTerminalBlock;
                        if (fat == null) continue;

                        var lcd = fat as Sandbox.ModAPI.IMyTextPanel;
                        if (lcd != null) lcds.Add(lcd);

                        if (fat.HasInventory) invBlocks.Add(fat);

                        var proj = fat as Sandbox.ModAPI.IMyProjector;
                        if (proj != null) projectors.Add(proj);
                    }

                    var tankInfos = BuildTankInfos(slims);

                    for (int i = 0; i < lcds.Count; i++)
                    {
                        var lcd = lcds[i];

                        string mode, token;
                        ParseFilter(lcd.CustomName ?? "", out mode, out token);

                        double capH2 = 0, amtH2 = 0, capO2 = 0, amtO2 = 0;
                        SumGasesFromTankInfos(tankInfos, token, ref capH2, ref amtH2, ref capO2, ref amtO2);

                        double inH2 = 0, outH2 = 0, inO2 = 0, outO2 = 0;
                        ComputeGasRates(grid, token, amtH2, amtO2, ref inH2, ref outH2, ref inO2, ref outO2);

                        var inTopH2  = new List<KeyValuePair<string,double>>();
                        var outTopH2 = new List<KeyValuePair<string,double>>();
                        var inTopO2  = new List<KeyValuePair<string,double>>();
                        var outTopO2 = new List<KeyValuePair<string,double>>();
                        BuildTopTankFlows(tankInfos, token, inTopH2, outTopH2, inTopO2, outTopO2);

                        int totalBlocks = 1, remainingBlocks = 0;
                        var missingMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        AggregateProjector(projectors, lcd.CustomName ?? "", ref totalBlocks, ref remainingBlocks, missingMap);

                    }
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }


        void ParseFilter(string name, out string mode, out string token)
        {
            mode = null; token = null;
            var mg = RxGroup.Match(name);
            if (mg.Success) { mode = "group"; token = mg.Groups[1].Value.Trim(); return; }
            var mc = RxContainer.Match(name);
            if (mc.Success) { mode = "container"; token = mc.Groups[1].Value.Trim(); return; }
        }

        string ResolveDisplayName(string typeIdString, string subtype)
        {
            try
            {
                VRage.Game.MyDefinitionId defId;
                if (MyDefinitionId.TryParse(typeIdString, subtype, out defId))
                {
                    var def = MyDefinitionManager.Static.GetDefinition(defId);
                    if (def != null)
                        return def.DisplayNameText; 
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, GetType());
            }
            return null;
        }

        string FallbackName(string subtype)
        {
            if (string.IsNullOrEmpty(subtype)) return "Desconhecido";
            var sb = new StringBuilder(subtype.Length + 8);
            for (int i = 0; i < subtype.Length; i++)
            {
                char c = subtype[i];
                if (i > 0 && char.IsUpper(c) && char.IsLower(subtype[i - 1]))
                    sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }

        void AggregateByType(List<IMyTerminalBlock> blocks, string token, Dictionary<string, double> dict, string typeSuffix)
        {
            dict.Clear();

            for (int b = 0; b < blocks.Count; b++)
            {
                var tb = blocks[b];

                if (!string.IsNullOrEmpty(token))
                {
                    var n = tb.CustomName ?? "";
                    if (n.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }

                if (!tb.HasInventory) continue;

                int invCount = tb.InventoryCount;
                for (int i = 0; i < invCount; i++)
                {
                    var inv = tb.GetInventory(i);
                    if (inv == null) continue;

                    var items = new List<IngameItem>();
                    inv.GetItems(items);
                    for (int k = 0; k < items.Count; k++)
                    {
                        var it = items[k];

                        var typeIdStr = it.Type.TypeId != null ? it.Type.TypeId.ToString() : "";
                        if (!typeIdStr.EndsWith(typeSuffix, StringComparison.OrdinalIgnoreCase)) continue;

                        string subtype = it.Type.SubtypeId ?? "";
                        string display = ResolveDisplayName(typeIdStr, subtype);
                        if (string.IsNullOrEmpty(display))
                            display = FallbackName(subtype);

                        double amount = (double)it.Amount;
                        if (amount <= 0) continue;

                        double acc;
                        if (dict.TryGetValue(display, out acc)) dict[display] = acc + amount;
                        else dict[display] = amount;
                    }
                }
            }
        }


        List<TankInfo> BuildTankInfos(List<IMySlimBlock> slims)
        {
            var list = new List<TankInfo>();
            double now = 0;
            try { now = MyAPIGateway.Session != null ? MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds : 0; } catch { }

            for (int s = 0; s < slims.Count; s++)
            {
                var tb = slims[s].FatBlock as IMyTerminalBlock;
                if (tb == null) continue;

                var gas = tb as Sandbox.ModAPI.IMyGasTank;
                if (gas == null) continue;

                double capacity = 0.0;
                string gasKind = null;
                string typeName = null;

                try
                {
                    var defBase = MyDefinitionManager.Static.GetCubeBlockDefinition(tb.BlockDefinition);
                    var gasDef = defBase as MyGasTankDefinition;
                    if (defBase != null) typeName = defBase.DisplayNameText;
                    if (gasDef != null)
                    {
                        capacity = (double)gasDef.Capacity;

                        var gid = gasDef.StoredGasId; // MyDefinitionId
                        var sub = gid.SubtypeName ?? gid.ToString();
                        if (!string.IsNullOrEmpty(sub))
                        {
                            var ssub = sub.ToLowerInvariant();
                            if (ssub.IndexOf("hydrogen", StringComparison.OrdinalIgnoreCase) >= 0) gasKind = "H2";
                            else if (ssub.IndexOf("oxygen", StringComparison.OrdinalIgnoreCase) >= 0) gasKind = "O2";
                        }
                    }
                }
                catch (Exception e)
                {
                    ErrorHandlerHelper.LogError(e, GetType());
                }

                if (gasKind == null)
                {
                    var name = (tb.CustomName ?? "").ToLowerInvariant();
                    if (name.Contains("hydrogen") || name.Contains("hidrogênio") || name.Contains("hidrogenio")) gasKind = "H2";
                    else if (name.Contains("oxygen") || name.Contains("oxigênio") || name.Contains("oxigenio")) gasKind = "O2";
                }
                if (string.IsNullOrEmpty(typeName)) typeName = tb.DefinitionDisplayNameText ?? (tb.BlockDefinition.SubtypeName ?? "Tanque");

                float ratio = 0f;
                try { ratio = (float) gas.FilledRatio; } catch { }
                double amount = capacity * (double)ratio;

                double rate = 0;
                var id = tb.EntityId;
                TankSnap snap;
                if (!_tankSnaps.TryGetValue(id, out snap))
                {
                    snap = new TankSnap { LastSec = now, LastAmount = amount };
                    _tankSnaps[id] = snap;
                }
                else
                {
                    var dt = now - snap.LastSec;
                    if (dt > 0.5)
                    {
                        rate = (amount - snap.LastAmount) / dt; 
                        snap.LastAmount = amount;
                        snap.LastSec = now;
                    }
                }

                var ti = new TankInfo
                {
                    Block = tb,
                    GasKind = gasKind,
                    TypeName = typeName,
                    Capacity = capacity,
                    Amount = amount,
                    Rate = rate
                };
                list.Add(ti);
            }

            return list;
        }

        void SumGasesFromTankInfos(List<TankInfo> tankInfos, string token,
                                   ref double capH2, ref double amtH2,
                                   ref double capO2, ref double amtO2)
        {
            capH2 = 0; amtH2 = 0; capO2 = 0; amtO2 = 0;
            for (int i = 0; i < tankInfos.Count; i++)
            {
                var t = tankInfos[i];
                if (t.GasKind == null) continue;

                if (!string.IsNullOrEmpty(token))
                {
                    var n = t.Block.CustomName ?? "";
                    if (n.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }

                if (t.GasKind == "H2") { capH2 += t.Capacity; amtH2 += t.Amount; }
                else if (t.GasKind == "O2") { capO2 += t.Capacity; amtO2 += t.Amount; }
            }
        }

        void BuildTopTankFlows(List<TankInfo> tankInfos, string token,
                               List<KeyValuePair<string,double>> inTopH2,
                               List<KeyValuePair<string,double>> outTopH2,
                               List<KeyValuePair<string,double>> inTopO2,
                               List<KeyValuePair<string,double>> outTopO2)
        {
            var inH2 = new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
            var outH2 = new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
            var inO2 = new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
            var outO2 = new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < tankInfos.Count; i++)
            {
                var t = tankInfos[i];
                if (t.GasKind == null) continue;

                if (!string.IsNullOrEmpty(token))
                {
                    var n = t.Block.CustomName ?? "";
                    if (n.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }

                var key = t.TypeName ?? "Tanque";
                if (t.GasKind == "H2")
                {
                    if (t.Rate > 0)
                    {
                        double acc; if (inH2.TryGetValue(key, out acc)) inH2[key] = acc + t.Rate; else inH2[key] = t.Rate;
                    }
                    else if (t.Rate < 0)
                    {
                        double acc; var v = -t.Rate;
                        if (outH2.TryGetValue(key, out acc)) outH2[key] = acc + v; else outH2[key] = v;
                    }
                }
                else if (t.GasKind == "O2")
                {
                    if (t.Rate > 0)
                    {
                        double acc; if (inO2.TryGetValue(key, out acc)) inO2[key] = acc + t.Rate; else inO2[key] = t.Rate;
                    }
                    else if (t.Rate < 0)
                    {
                        double acc; var v = -t.Rate;
                        if (outO2.TryGetValue(key, out acc)) outO2[key] = acc + v; else outO2[key] = v;
                    }
                }
            }

            ToTop3(inH2, inTopH2);
            ToTop3(outH2, outTopH2);
            ToTop3(inO2, inTopO2);
            ToTop3(outO2, outTopO2);
        }

        void ToTop3(Dictionary<string,double> src, List<KeyValuePair<string,double>> dst)
        {
            dst.Clear();
            var tmp = new List<KeyValuePair<string,double>>(src);
            tmp.Sort((a,b) => b.Value.CompareTo(a.Value));
            for (int i = 0; i < tmp.Count && i < 3; i++) dst.Add(tmp[i]);
        }

        void ComputeGasRates(IMyCubeGrid grid, string token, double amtH2, double amtO2,
                             ref double inH2, ref double outH2, ref double inO2, ref double outO2)
        {
            inH2 = 0; outH2 = 0; inO2 = 0; outO2 = 0;

            double t = 0;
            try { t = MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds; } catch { }

            string key = (grid != null ? grid.EntityId.ToString() : "0") + "|" + (token ?? "*") + "|GAS";
            GasSnapshot snap;
            if (!_gasSnaps.TryGetValue(key, out snap))
            {
                snap = new GasSnapshot { LastSec = t, LastH2 = amtH2, LastO2 = amtO2 };
                _gasSnaps[key] = snap;
                return;
            }

            double dt = t - snap.LastSec;
            if (dt <= 0.5)
            {
                snap.LastH2 = amtH2;
                snap.LastO2 = amtO2;
                snap.LastSec = t;
                return;
            }

            double dH2 = amtH2 - snap.LastH2;
            double dO2 = amtO2 - snap.LastO2;

            if (dH2 >= 0) inH2 = dH2 / dt; else outH2 = (-dH2) / dt;
            if (dO2 >= 0) inO2 = dO2 / dt; else outO2 = (-dO2) / dt;

            snap.LastH2 = amtH2;
            snap.LastO2 = amtO2;
            snap.LastSec = t;
        }

        void AggregateProjector(List<Sandbox.ModAPI.IMyProjector> projectors, string lcdName, ref int total, ref int remaining, Dictionary<string, int> missing)
        {
            total = 1; remaining = 0; missing.Clear();

            string token = null;
            var mp = RxProjToken.Match(lcdName ?? "");
            if (mp.Success) token = mp.Groups[1].Value.Trim();

            Sandbox.ModAPI.IMyProjector target = null;
            for (int i = 0; i < projectors.Count; i++)
            {
                var p = projectors[i] as IMyTerminalBlock;
                if (p == null) continue;

                if (!string.IsNullOrEmpty(token))
                {
                    var name = p.CustomName ?? "";
                    if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }
                target = projectors[i];
                break;
            }

            if (target == null) return;

            try
            {
                total     = target.TotalBlocks;
                remaining = target.RemainingBlocks;
            }
            catch { total = 1; remaining = 0; }

            try
            {
                var term = target as IMyTerminalBlock;
                var info = term != null ? (term.DetailedInfo ?? "") : "";
                ParseMissingFromText(info, missing);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, GetType());
            }
        }

        void ParseMissingFromText(string text, Dictionary<string,int> missing)
        {
            if (string.IsNullOrEmpty(text)) return;

            var lines = text.Split(new[] { '\r','\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                var m = RxMissA.Match(line);
                if (m.Success) { AddMissing(missing, m.Groups[1].Value, m.Groups[2].Value); continue; }

                m = RxMissB.Match(line);
                if (m.Success) { AddMissing(missing, m.Groups[2].Value, m.Groups[1].Value); continue; }

                m = RxMissC.Match(line);
                if (m.Success) { AddMissing(missing, m.Groups[1].Value, m.Groups[2].Value); continue; }
            }
        }

        void AddMissing(Dictionary<string,int> map, string rawName, string rawQty)
        {
            var name = (rawName ?? "").Trim();
            var digits = new StringBuilder();
            for (int i = 0; i < rawQty.Length; i++)
            {
                char c = rawQty[i];
                if (c >= '0' && c <= '9') digits.Append(c);
            }
            int qty = 0;
            int.TryParse(digits.ToString(), out qty);
            if (qty <= 0 || string.IsNullOrEmpty(name)) return;

            int acc;
            if (map.TryGetValue(name, out acc)) map[name] = acc + qty;
            else map[name] = qty;
        }
    }
}
