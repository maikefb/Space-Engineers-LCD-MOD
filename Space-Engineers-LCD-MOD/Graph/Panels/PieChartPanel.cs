using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Graph.Data.Scripts.Graph.Panels
{
    public class PieChartPanel
    {
        private readonly IMyTextSurface _surface;
        private readonly string _title;
        private readonly Vector2 _origo;
        private readonly Vector2 _size;
        private readonly bool _showTitle;
        private readonly List<MySprite> _sprites = new List<MySprite>();
        private readonly Color _colorHeader;

        public PieChartPanel(string title, IMyTextSurface surface, Vector2 margin, Vector2 size, bool showTitle = true,
            Color? configHeaderColor = null)
        {
            _title = title ?? "";
            _surface = surface;
            _size = size;
            _showTitle = showTitle;
            _colorHeader = configHeaderColor ?? surface.ScriptForegroundColor;
            _origo = new Vector2(margin.X, 512 - margin.Y); 
            
        }

        public List<MySprite> GetSprites(float value, bool turnDarkOnComplete = false)
        {
            _sprites.Clear();
            if (_showTitle) DrawTitle(value, turnDarkOnComplete);
            DrawPie(value, turnDarkOnComplete);
            return _sprites;
        }

        private void DrawPie(float value, bool turnDarkOnComplete)
        {
            Vector2 position = _origo - (_size / 2);
            int r = (int)(_surface.ScriptForegroundColor.R * 0.5f);
            int g = (int)(_surface.ScriptForegroundColor.G * 0.5f);
            int b = (int)(_surface.ScriptForegroundColor.B * 0.5f);
            
            int headerR = (int)(_colorHeader.R * 0.5f);
            int headerG = (int)(_colorHeader.G * 0.5f);
            int headerB = (int)(_colorHeader.B * 0.5f);

            float deg = 360 * value;
            float flip = value < 0.5f ? 1 : -1;

            // Fundo
            _sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = position,
                Size = _size,
                Color = deg > 358 && turnDarkOnComplete ? _surface.ScriptForegroundColor : new Color(r, g, b),
                Alignment = TextAlignment.LEFT
            });

            if (deg > 358) return;

            float val = value < 0.5f ? 180 : 0;

            // Cobertura 1
            _sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SemiCircle",
                Position = position,
                Size = _size,
                Color = _colorHeader,
                RotationOrScale = MathHelper.ToRadians((flip * 90) + deg - val),
                Alignment = TextAlignment.LEFT
            });

            // Cobertura 2
            _sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SemiCircle",
                Position = position,
                Size = _size,
                Color = value > 0.5f ? _colorHeader : new Color(r, g, b),
                RotationOrScale = MathHelper.ToRadians(flip * (-90)),
                Alignment = TextAlignment.LEFT
            });
        }

        private void DrawTitle(float value, bool turnDarkOnComplete)
        {
            Vector2 titleSize = new Vector2(_size.X, 18);
            _sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareSimple",
                Position = _origo - new Vector2(_size.X, _size.Y + (titleSize.Y / 2) + 10),
                Size = new Vector2(_size.X * 2, titleSize.Y),
                Color = new Color(0, 0, 0, 140),
                Alignment = TextAlignment.LEFT
            });

            _sprites.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = _title + " (" + (value * 100).ToString("F0") + "%)",
                Position = _origo - new Vector2(_size.X - 4, _size.Y + (titleSize.Y / 2) + 16),
                Color = _colorHeader,
                Alignment = TextAlignment.LEFT,
                RotationOrScale = 0.55f
            });
        }
    }
}
