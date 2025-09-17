using System;
using System.Collections.Generic;

using Graph.Data.Scripts.Graph.Sys;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace Graph.Data.Scripts.Graph
{
    [MyTextSurfaceScript("ConsumablesCharts", "Consumíveis")]
    public class ConsumablesCharts : ItemCharts
    {
        public override Dictionary<string, double> ItemSource => GridLogic != null ? GridLogic.Consumables : null;

        private static readonly Vector2 TITLE_POS = new Vector2(16, 20);
        private static readonly Vector2 INFO_POS  = new Vector2(16, 50);
        private const float LINE = 18f;
        private const int SCROLL_SECONDS = 2;

        public new IMyTextSurface Surface { get; set; }
        public new IMyCubeBlock  Block { get; set; }
        public override ScriptUpdate NeedsUpdate { get { return ScriptUpdate.Update10; } }

        public ConsumablesCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            Surface = surface;
            Block   = block;
            Surface.ContentType = ContentType.SCRIPT;
        }

        public override void Run()
        {
            base.Run();

            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();

                string mode, token;
                ParseFilter(Block as IMyTerminalBlock, out mode, out token);

                var title = "Consumíveis";
                if (!string.IsNullOrEmpty(token))
                    title += (mode == "group") ? ("  ·  (G: " + token + ")") : ("  ·  (" + token + ")");
                sprites.Add(MakeText(Surface, title, TITLE_POS, 0.95f));

                var items = SortedItems(ItemSource);

                var pos = INFO_POS;
                sprites.Add(MakeText(Surface, "Estoque de consumíveis", pos, 0.85f));
                pos += new Vector2(0, LINE);
                pos += new Vector2(0, LINE);

                if (items.Count == 0)
                {
                    sprites.Add(MakeText(Surface, "- sem dados -", pos, 0.78f));
                }
                else
                {
                    int maxRows = GetMaxRows(Surface, pos.Y, LINE);
                    if (maxRows < 1) maxRows = 1;

                    bool shouldScroll = items.Count > (int)Math.Floor(maxRows * 0.95);
                    int visible = maxRows;
                    int start = 0;

                    if (shouldScroll && items.Count > visible)
                    {
                        int step = GetScrollStep(SCROLL_SECONDS);
                        start = step % items.Count;
                    }

                    int showCount = Math.Min(visible, items.Count);
                    for (int visIdx = 0; visIdx < showCount; visIdx++)
                    {
                        int realIdx = (start + visIdx) % items.Count;
                        var p = pos + new Vector2(0f, visIdx * LINE);
                        string line = items[realIdx].Key + ": " + FormatQty(items[realIdx].Value);
                        sprites.Add(MakeText(Surface, line, p, 0.78f));
                    }
                }

                frame.AddRange(sprites);
            }
        }
    }
}
