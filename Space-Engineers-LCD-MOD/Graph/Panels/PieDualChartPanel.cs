using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Graph.Data.Scripts.Graph.Panels
{
    public class PieDualChartPanel : PieChartPanel
    {
        public PieDualChartPanel(string title, IMyTextSurface surface, Vector2 margin, Vector2 size,
            bool showTitle = true) : base(title, surface, margin, size, showTitle)
        {
        }

        public List<MySprite> GetSprites(float value, float value2, Color? color = null,
            bool turnDarkOnComplete = false)
        {
            if (color == null)
                color = _surface.ScriptForegroundColor;

            _sprites.Clear();

            if (_showTitle) DrawTitle(value, color.Value);
            DrawBackground(value2, color.Value, _surface.ScriptForegroundColor, turnDarkOnComplete);
            if (value2 > .99)
                return _sprites;

            DrawPie(value, _surface.ScriptForegroundColor, _surface.ScriptForegroundColor);
            DrawPie(value2, color.Value, _surface.ScriptForegroundColor);

            return _sprites;
        }
    }
}