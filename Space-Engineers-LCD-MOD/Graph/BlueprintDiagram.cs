using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Graph.Data.Scripts.Graph.Panels;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Helpers;
using VRage;
using VRageMath;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Utils;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("BlueprintDiagram", "Blueprints")]
    public class BlueprintDiagram : ItemCharts
    {
        public override string Title
        {
            get
            {
                if (_projector != null && !string.IsNullOrEmpty(_projector.CustomName))
                    return _projector.CustomName;

                return DefaultTitle;
            }
        }

        protected override string DefaultTitle { get; set; } = "Blueprints";

        IMyProjector _projector;

        public override Dictionary<MyItemType, double> ItemSource => _shortage;

        readonly Dictionary<MyItemType, double> _shortage = new Dictionary<MyItemType, double>();

        int _totalBlocks = 1;
        int _remainingBlocks = 0;

        int _totalComponents = 0;
        int _missingComponents = 0;

        readonly Vector2 _piePosition = new Vector2(10 + PIE_RADIUS / 2, -5);
        const float PIE_RADIUS = 40;
        PieDualChartPanel _pieBlueprint;

        static readonly Regex RxMissA = new Regex(@"^\s*([\p{L}0-9][\p{L}0-9 _\.\-]+?)\s*[x×]\s*([0-9][0-9\.\, ]*)\s*$",
            RegexOptions.IgnoreCase);

        static readonly Regex RxMissB = new Regex(@"^\s*([0-9][0-9\.\, ]*)\s*[x×]\s*([\p{L}0-9][\p{L}0-9 _\.\-]+?)\s*$",
            RegexOptions.IgnoreCase);

        static readonly Regex RxMissC = new Regex(@"^\s*([\p{L}0-9][\p{L}0-9 _\.\-]+?)\s*:\s*([0-9][0-9\.\, ]*)\s*$",
            RegexOptions.IgnoreCase);

        static Dictionary<string, MyItemType> _componentLookup;
        static Dictionary<string, MyItemType> _componentLookupNormalized;
        static List<KeyValuePair<string, MyItemType>> _componentListNormalized;

        static Dictionary<string, Dictionary<MyItemType, int>> _blockToCompsRaw;
        static Dictionary<string, Dictionary<MyItemType, int>> _blockToCompsNorm;
        static List<KeyValuePair<string, Dictionary<MyItemType, int>>> _blockListNormalized;

        public BlueprintDiagram(IMyTextSurface surface, IMyCubeBlock block, Vector2 size)
            : base(surface, block, size)
        {
            _pieBlueprint = new PieDualChartPanel(
                "",
                (IMyTextSurface)Surface,
                ToScreenMargin(new Vector2(ViewBox.Position.X, ViewBox.Bottom) + _piePosition * Scale),
                new Vector2(PIE_RADIUS * Scale),
                false
            );
        }

        protected override void LayoutChanged()
        {
            base.LayoutChanged();
            _pieBlueprint.SetMargin(
                ToScreenMargin(new Vector2(ViewBox.Position.X, ViewBox.Bottom) + _piePosition * Scale),
                new Vector2(PIE_RADIUS * Scale));
        }

        protected override void DrawFooter(List<MySprite> frame)
        {
            EnsureData();

            if (_projector == null)
                return;

            if (_totalBlocks == 0 || _totalComponents == 0)
                return;

            var margin = ViewBox.Size.X * 0.02f;
            var pos = ViewBox.Position;
            pos.X += margin;

            int built = Math.Max(_totalBlocks - _remainingBlocks, 0);

            FooterHeight = (25f * 2) * Scale;
            pos.X += (25f * 2) * Scale;

            pos.Y = ViewBox.Bottom - FooterHeight;

            var legendSize = new Vector2(8, 8) * Scale;
            
            var blocksString = MyTexts.GetString("TerminalTab_Info_Blocks");

            pos.X += legendSize.X;

            var lineSpacer = 25f * Scale;

            var blocksPct = built / (float)_totalBlocks;
            var componentsPct = 1 - (float)_missingComponents / _totalComponents;

            StringBuilder sb =
                new StringBuilder($"{blocksString}{blocksPct:P2}  ({built}/{_totalBlocks} )");

            TrimText(ref sb, ViewBox.Width - pos.X - ViewBox.X, 0.9f);

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = sb.ToString(),
                Position = pos,
                RotationOrScale = Scale * 0.9f,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            pos.Y += lineSpacer;

            var components = MyTexts.GetString("DisplayName_InventoryConstraint_Components");

            sb.Clear();
            sb.Append(
                $"{components}: {componentsPct:P2}  ({(_totalComponents - _missingComponents).ToString(CultureInfo.CurrentUICulture)}" +
                $"/{_totalComponents.ToString(CultureInfo.CurrentUICulture)})");


            TrimText(ref sb, ViewBox.Width - pos.X - ViewBox.X, 0.9f);

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = sb.ToString(),
                Position = pos,
                RotationOrScale = Scale * 0.9f,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            pos.X -= legendSize.X;

            pos.Y -= lineSpacer - (legendSize.Y + legendSize.Y / 2);

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = pos,
                Size = legendSize,
                Color = Config.HeaderColor,
                Alignment = TextAlignment.CENTER,
            });

            pos.Y += lineSpacer;

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = pos,
                Size = legendSize,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.CENTER,
            });

            frame.AddRange(_pieBlueprint.GetSprites(componentsPct, blocksPct, Config.HeaderColor, true));
        }

        void EnsureData()
        {
            _shortage.Clear();
            _totalBlocks = 1;
            _remainingBlocks = 0;
            _totalComponents = 0;
            _missingComponents = 0;

            var lcd = Block as IMyTerminalBlock;

            IMyCubeGrid grid = Block?.CubeGrid as IMyCubeGrid;

            if (grid == null)
                return;

            _projector = FindProjector(grid);

            if (_projector == null)
                return;

            try
            {
                _totalBlocks = Math.Max(_projector.TotalBlocks, 1);
                _remainingBlocks = Math.Max(_projector.RemainingBlocks, 0);
            }
            catch
            {
                _totalBlocks = 1;
                _remainingBlocks = 0;
            }

            var needByType = new Dictionary<MyItemType, long>();
            try
            {
                var term = _projector as IMyTerminalBlock;
                var info = term != null ? (term.DetailedInfo ?? "") : "";
                var missingPairs = ParseMissing(info);

                EnsureComponentLookup();
                EnsureBlockLookup();

                for (int i = 0; i < missingPairs.Count; i++)
                {
                    var kv = missingPairs[i];
                    long qty = kv.Value;

                    MyItemType compType;
                    if (TryResolveComponentType(kv.Key, out compType))
                    {
                        AddAmount(needByType, compType, qty);
                        continue;
                    }

                    Dictionary<MyItemType, int> blockComps;
                    if (TryResolveBlockComponents(kv.Key, out blockComps))
                    {
                        foreach (var bc in blockComps)
                            AddAmount(needByType, bc.Key, (long)bc.Value * qty);
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, GetType());
            }

            var haveByType = GetAvailableComponents(lcd);

            long totalNeed = 0;
            long totalShort = 0;

            foreach (var kv in needByType)
            {
                double have = 0;
                haveByType.TryGetValue(kv.Key, out have);

                double shortVal = kv.Value - have;
                if (shortVal < 0) shortVal = 0;

                if (shortVal > 0)
                    _shortage[kv.Key] = shortVal;

                totalNeed += kv.Value;
                totalShort += (long)Math.Round(shortVal);
            }

            _totalComponents = (int)Math.Max(0, totalNeed);
            _missingComponents = (int)Math.Max(0, totalShort);
        }

        static void AddAmount(Dictionary<MyItemType, long> dict, MyItemType type, long amount)
        {
            long acc;
            if (dict.TryGetValue(type, out acc)) dict[type] = acc + amount;
            else dict[type] = amount;
        }

        Dictionary<MyItemType, double> GetAvailableComponents(IMyTerminalBlock referenceBlock)
        {
            var have = new Dictionary<MyItemType, double>();

            try
            {
                bool hasSelection =
                    (Config != null) &&
                    ((Config.SelectedBlocks != null && Config.SelectedBlocks.Length > 0) ||
                     (Config.SelectedGroups != null && Config.SelectedGroups.Length > 0));

                if (hasSelection && GridLogic != null && referenceBlock != null)
                {
                    var all = GridLogic.GetItems(Config, referenceBlock);
                    if (all != null)
                    {
                        foreach (var kv in all)
                        {
                            bool isComponent = false;
                            try
                            {
                                if (kv.Key.TypeId == "MyObjectBuilder_Component") isComponent = true;
                            }
                            catch
                            {
                                var s = kv.Key.ToString();
                                if (!string.IsNullOrEmpty(s) && s.IndexOf("MyObjectBuilder_Component",
                                        StringComparison.OrdinalIgnoreCase) >= 0)
                                    isComponent = true;
                            }

                            if (!isComponent) continue;

                            double acc;
                            if (have.TryGetValue(kv.Key, out acc)) have[kv.Key] = acc + kv.Value;
                            else have[kv.Key] = kv.Value;
                        }
                    }
                }
                else
                {
                    if (GridLogic != null && GridLogic.Components != null)
                    {
                        foreach (var kv in GridLogic.Components)
                            have[kv.Key] = kv.Value;
                    }
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, GetType());
            }

            return have;
        }

        IMyProjector FindProjector(IMyCubeGrid grid)
        {
            if (Config.ReferenceBlock == 0)
                return null;
            var entity = MyAPIGateway.Entities.GetEntityById(Config.ReferenceBlock);

            var projector = entity as IMyProjector;
            if (projector == null)
                return null;

            return projector.CubeGrid.IsInSameLogicalGroupAs(grid) ? projector : null;
        }

        static string NormalizeDetailedInfoToList(string detailedInfo)
        {
            if (string.IsNullOrWhiteSpace(detailedInfo))
                return string.Empty;

            var rawLines = detailedInfo.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var listLines = new List<string>(64);
            for (int i = 0; i < rawLines.Length; i++)
            {
                var line = rawLines[i].Trim();
                if (line.Length == 0) continue;

                var parts = line.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int p = 0; p < parts.Length; p++)
                {
                    var part = parts[p].Trim();
                    if (part.Length > 0)
                        listLines.Add(part);
                }
            }

            return string.Join("\n", listLines);
        }

        List<KeyValuePair<string, int>> ParseMissing(string detailedInfo)
        {
            var list = new List<KeyValuePair<string, int>>();
            if (string.IsNullOrEmpty(detailedInfo)) return list;

            var normalized = NormalizeDetailedInfoToList(detailedInfo);
            var lines = normalized.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;

                var m = RxMissA.Match(line);
                if (m.Success)
                {
                    AddMissing(list, m.Groups[1].Value, m.Groups[2].Value);
                    continue;
                }

                m = RxMissB.Match(line);
                if (m.Success)
                {
                    AddMissing(list, m.Groups[2].Value, m.Groups[1].Value);
                    continue;
                }

                m = RxMissC.Match(line);
                if (m.Success)
                {
                    AddMissing(list, m.Groups[1].Value, m.Groups[2].Value);
                    continue;
                }

                var mfallback = Regex.Match(line, @"^\s*([\p{L}0-9][\p{L}0-9 _\.\-]+?)\s+([0-9][0-9\.\,\s]*)\s*$",
                    RegexOptions.IgnoreCase);
                if (mfallback.Success)
                {
                    AddMissing(list, mfallback.Groups[1].Value, mfallback.Groups[2].Value);
                }
            }

            var agg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < list.Count; i++)
            {
                var kv = list[i];
                int acc;
                if (agg.TryGetValue(kv.Key, out acc)) agg[kv.Key] = acc + kv.Value;
                else agg[kv.Key] = kv.Value;
            }

            var result = new List<KeyValuePair<string, int>>(agg.Count);
            foreach (var kv in agg) result.Add(kv);
            result.Sort((a, b) => b.Value.CompareTo(a.Value));
            return result;
        }

        void AddMissing(List<KeyValuePair<string, int>> dst, string rawName, string rawQty)
        {
            var name = (rawName ?? "").Trim();
            if (string.IsNullOrEmpty(name)) return;

            string digits = "";
            int len = rawQty != null ? rawQty.Length : 0;
            for (int i = 0; i < len; i++)
            {
                char c = rawQty[i];
                if (c >= '0' && c <= '9') digits += c;
            }

            int qty = 0;
            int.TryParse(digits, out qty);
            if (qty <= 0) return;

            dst.Add(new KeyValuePair<string, int>(name, qty));
        }

        static string NormalizeKey(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            string formD = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            for (int i = 0; i < formD.Length; i++)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(formD[i]);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(formD[i]);
            }

            var noDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);

            var sb2 = new StringBuilder(noDiacritics.Length);
            for (int i = 0; i < noDiacritics.Length; i++)
            {
                char c = noDiacritics[i];
                if (char.IsLetterOrDigit(c))
                    sb2.Append(char.ToLowerInvariant(c));
            }

            return sb2.ToString();
        }

        static void AddCompKey(Dictionary<string, MyItemType> dict, Dictionary<string, MyItemType> dictNorm, string key,
            MyItemType type)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (!dict.ContainsKey(key))
                dict[key] = type;

            var nk = NormalizeKey(key);
            if (!string.IsNullOrEmpty(nk) && !dictNorm.ContainsKey(nk))
                dictNorm[nk] = type;
        }

        void EnsureComponentLookup()
        {
            if (_componentLookup != null && _componentLookupNormalized != null && _componentListNormalized != null)
                return;

            var raw = new Dictionary<string, MyItemType>(StringComparer.OrdinalIgnoreCase);
            var norm = new Dictionary<string, MyItemType>();

            try
            {
                var defs = MyDefinitionManager.Static.GetAllDefinitions();
                foreach (var def in defs)
                {
                    var comp = def as MyComponentDefinition;
                    if (comp == null) continue;

                    var subtype = comp.Id.SubtypeName ?? "";
                    var type = new MyItemType("MyObjectBuilder_Component", subtype);

                    if (!string.IsNullOrEmpty(comp.DisplayNameText))
                        AddCompKey(raw, norm, comp.DisplayNameText, type);

                    try
                    {
                        if (comp.DisplayNameEnum.HasValue)
                        {
                            var loc = MyTexts.GetString(comp.DisplayNameEnum.Value);
                            if (!string.IsNullOrEmpty(loc))
                                AddCompKey(raw, norm, loc, type);
                        }
                    }
                    catch
                    {
                    }

                    if (!string.IsNullOrEmpty(subtype))
                    {
                        AddCompKey(raw, norm, subtype, type);
                        AddCompKey(raw, norm, subtype.Replace(" ", ""), type);
                        AddCompKey(raw, norm, subtype.Replace("_", " "), type);
                        AddCompKey(raw, norm, subtype.Replace("_", ""), type);
                    }
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, GetType());
            }

            _componentLookup = raw;
            _componentLookupNormalized = norm;
            _componentListNormalized = new List<KeyValuePair<string, MyItemType>>(norm);
        }

        bool TryResolveComponentType(string displayOrSubtype, out MyItemType type)
        {
            type = default(MyItemType);
            if (_componentLookup == null || _componentLookupNormalized == null) return false;

            MyItemType t;
            if (_componentLookup.TryGetValue(displayOrSubtype, out t))
            {
                type = t;
                return true;
            }

            var nk = NormalizeKey(displayOrSubtype);
            if (!string.IsNullOrEmpty(nk) && _componentLookupNormalized.TryGetValue(nk, out t))
            {
                type = t;
                return true;
            }

            if (!string.IsNullOrEmpty(nk))
            {
                for (int i = 0; i < _componentListNormalized.Count; i++)
                {
                    var kv = _componentListNormalized[i];
                    if (kv.Key.IndexOf(nk, StringComparison.Ordinal) >= 0)
                    {
                        type = kv.Value;
                        return true;
                    }
                }
            }

            return false;
        }

        static void AddBlockKey(string key,
            Dictionary<MyItemType, int> compMap,
            Dictionary<string, Dictionary<MyItemType, int>> raw,
            Dictionary<string, Dictionary<MyItemType, int>> norm)
        {
            if (string.IsNullOrEmpty(key) || compMap == null || compMap.Count == 0) return;

            if (!raw.ContainsKey(key))
                raw[key] = compMap;

            var nk = NormalizeKey(key);
            if (!string.IsNullOrEmpty(nk) && !norm.ContainsKey(nk))
                norm[nk] = compMap;
        }

        static void AccumulateComp(Dictionary<MyItemType, int> dst, MyItemType t, int count)
        {
            int acc;
            if (dst.TryGetValue(t, out acc)) dst[t] = acc + count;
            else dst[t] = count;
        }

        void EnsureBlockLookup()
        {
            if (_blockToCompsRaw != null && _blockToCompsNorm != null && _blockListNormalized != null)
                return;

            var raw = new Dictionary<string, Dictionary<MyItemType, int>>(StringComparer.OrdinalIgnoreCase);
            var norm = new Dictionary<string, Dictionary<MyItemType, int>>();

            try
            {
                var defs = MyDefinitionManager.Static.GetAllDefinitions();
                foreach (var d in defs)
                {
                    var cbd = d as MyCubeBlockDefinition;
                    if (cbd == null) continue;

                    var map = new Dictionary<MyItemType, int>();
                    try
                    {
                        var comps = cbd.Components;
                        if (comps != null)
                        {
                            for (int i = 0; i < comps.Length; i++)
                            {
                                var cd = comps[i].Definition as MyComponentDefinition;
                                int cnt = comps[i].Count;
                                if (cd == null || cnt <= 0) continue;

                                var subtype = cd.Id.SubtypeName ?? "";
                                if (string.IsNullOrEmpty(subtype)) continue;

                                var it = new MyItemType("MyObjectBuilder_Component", subtype);
                                AccumulateComp(map, it, cnt);
                            }
                        }
                    }
                    catch
                    {
                    }

                    if (map.Count == 0) continue;

                    if (!string.IsNullOrEmpty(cbd.DisplayNameText))
                        AddBlockKey(cbd.DisplayNameText, map, raw, norm);

                    try
                    {
                        if (cbd.DisplayNameEnum.HasValue)
                        {
                            var loc = MyTexts.GetString(cbd.DisplayNameEnum.Value);
                            if (!string.IsNullOrEmpty(loc))
                                AddBlockKey(loc, map, raw, norm);
                        }
                    }
                    catch
                    {
                    }

                    var sub = cbd.Id.SubtypeName ?? "";
                    if (!string.IsNullOrEmpty(sub))
                    {
                        AddBlockKey(sub, map, raw, norm);
                        AddBlockKey(sub.Replace(" ", ""), map, raw, norm);
                        AddBlockKey(sub.Replace("_", " "), map, raw, norm);
                        AddBlockKey(sub.Replace("_", ""), map, raw, norm);
                    }
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, GetType());
            }

            _blockToCompsRaw = raw;
            _blockToCompsNorm = norm;
            _blockListNormalized = new List<KeyValuePair<string, Dictionary<MyItemType, int>>>(norm);
        }

        bool TryResolveBlockComponents(string name, out Dictionary<MyItemType, int> compMap)
        {
            compMap = null;
            if (_blockToCompsRaw == null || _blockToCompsNorm == null) return false;

            Dictionary<MyItemType, int> m;
            if (_blockToCompsRaw.TryGetValue(name, out m))
            {
                compMap = m;
                return true;
            }

            var nk = NormalizeKey(name);
            if (!string.IsNullOrEmpty(nk) && _blockToCompsNorm.TryGetValue(nk, out m))
            {
                compMap = m;
                return true;
            }

            if (!string.IsNullOrEmpty(nk))
            {
                for (int i = 0; i < _blockListNormalized.Count; i++)
                {
                    var kv = _blockListNormalized[i];
                    if (kv.Key.IndexOf(nk, StringComparison.Ordinal) >= 0)
                    {
                        compMap = kv.Value;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}