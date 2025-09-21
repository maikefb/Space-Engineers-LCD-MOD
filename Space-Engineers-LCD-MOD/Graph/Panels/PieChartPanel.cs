using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Graph.Data.Scripts.Graph.Panels
{
    public class PieChartPanel
    {
        protected readonly IMyTextSurface _surface;
        protected readonly string _title;
        protected Vector2 _origo;
        protected Vector2 _size;
        protected readonly bool _showTitle;
        protected readonly List<MySprite> _sprites = new List<MySprite>();

        public PieChartPanel(string title, IMyTextSurface surface, Vector2 margin, Vector2 size, bool showTitle = true)
        {
            _title = title ?? "";
            _surface = surface;
            _showTitle = showTitle;
            SetMargin(margin, size);
        }

        public void SetMargin(Vector2 margin, Vector2 size)
        {
            _origo = new Vector2(margin.X, 512 - margin.Y);
            _size = size;
        }

        public virtual List<MySprite> GetSprites(float value, Color? color = null, bool turnDarkOnComplete = false)
        {
            if (color == null)
                color = _surface.ScriptForegroundColor;
            _sprites.Clear();
            if (_showTitle) DrawTitle(value, color.Value);
            DrawBackground(value, color.Value, _surface.ScriptForegroundColor, turnDarkOnComplete);
            if (value > .99) 
                return _sprites;
            DrawPie(value, color.Value, _surface.ScriptForegroundColor);
            return _sprites;
        }

        protected virtual void DrawBackground(float value, Color color, Color backgroundColor, bool turnDarkOnComplete)
        {
            Vector2 position = _origo - (_size / 2);

            float deg = 360 * value;

            _sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = position,
                Size = _size,
                Color = deg > 358 && turnDarkOnComplete ? color : DarkenColor(backgroundColor),
                Alignment = TextAlignment.LEFT
            });
        }
        
        protected virtual void DrawPie(float value, Color color, Color backgroundColor)
        {
            Vector2 position = _origo - (_size / 2);

            float deg = 360 * value;
            float flip = value < 0.5f ? 1 : -1;

            if (value > .99) 
                return;

            float val = value < 0.5f ? 180 : 0;

            // Cover 1
            _sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SemiCircle",
                Position = position,
                Size = _size,
                Color = color,
                RotationOrScale = MathHelper.ToRadians((flip * 90) + deg - val),
                Alignment = TextAlignment.LEFT
            });

            // Cover 2
            _sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SemiCircle",
                Position = position,
                Size = _size,
                Color = value > 0.5f ? color : DarkenColor(backgroundColor),
                RotationOrScale = MathHelper.ToRadians(flip * (-90)),
                Alignment = TextAlignment.LEFT
            });
        }

        Color DarkenColor(Color color) => new Color((int)(color.R * 0.5f), (int)(color.G * 0.5f), (int)(color.B * 0.5f), color.A);

        protected virtual void DrawTitle(float value, Color color)
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
                Color = color,
                Alignment = TextAlignment.LEFT,
                RotationOrScale = 0.55f
            });
        }
    }
}