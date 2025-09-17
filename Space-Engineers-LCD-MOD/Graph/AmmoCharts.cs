using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Graph.Data.Scripts.Graph.Sys;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("AmmoCharts", "DisplayName_BlueprintClass_Ammo")]
    public class AmmoCharts : ItemCharts
    {
        public override Dictionary<MyItemType, double> ItemSource => GridLogic?.Ammo;
        public override string Title { get; protected set; } = "DisplayName_BlueprintClass_Ammo";
        
        private const float LINE = 18f;
        private const int SCROLL_SECONDS = 2;

        private static readonly Regex RxGroup     = new Regex(@"\(\s*G\s*:\s*(.+?)\s*\)", RegexOptions.IgnoreCase);
        private static readonly Regex RxContainer = new Regex(@"\(\s*(?!G\s*:)(.+?)\s*\)", RegexOptions.IgnoreCase);

        public AmmoCharts(IMyTextSurface surface, VRage.Game.ModAPI.Ingame.IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        { }

        private int GetScrollStep(int secondsPerStep)
        {
            try { var sess = MyAPIGateway.Session; if (sess == null) return 0; if (secondsPerStep <= 0) secondsPerStep = 1; double sec = sess.ElapsedPlayTime.TotalSeconds; return (int)(sec / secondsPerStep); }
            catch { return 0; }
        }

        private int GetMaxRowsFromSurface(float listStartY)
        {
            float surfH = 512f; try { surfH = Surface.SurfaceSize.Y; } catch { }
            float available = Math.Max(0f, surfH - listStartY - 10f);
            int rows = (int)Math.Floor(available / LINE);
            return rows < 1 ? 1 : rows;
        }

        private void ParseFilter(IMyTerminalBlock lcd, out string mode, out string token)
        {
            mode = null; token = null; if (lcd == null) return;
            var name = lcd.CustomName ?? string.Empty;
            var mg = RxGroup.Match(name); if (mg.Success) { mode = "group"; token = mg.Groups[1].Value.Trim(); return; }
            var mc = RxContainer.Match(name); if (mc.Success) { mode = "container"; token = mc.Groups[1].Value.Trim(); return; }
        }
    }
}
