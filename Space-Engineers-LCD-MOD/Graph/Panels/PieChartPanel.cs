using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Graph.Data.Scripts.Graph.Panels
{
    public class PieChartPanel
    {
        private readonly IMyTextSurface surface;
        private readonly string title;
        private readonly Vector2 origo;
        private readonly Vector2 size;
        private readonly bool showTitle;
        private readonly List<MySprite> sprites = new List<MySprite>();

        public PieChartPanel(string title, IMyTextSurface surface, Vector2 margin, Vector2 size, bool showTitle = true)
        {
            this.title = title ?? "";
            this.surface = surface;
            this.size = size;
            this.showTitle = showTitle;
            origo = new Vector2(margin.X, 512 - margin.Y); 
        }

        public List<MySprite> GetSprites(float value, bool turnDarkOnComplete = false)
        {
            sprites.Clear();
            if (showTitle) DrawTitle(value, turnDarkOnComplete);
            DrawPie(value, turnDarkOnComplete);
            return sprites;
        }

        private void DrawPie(float value, bool turnDarkOnComplete)
        {
            Vector2 position = origo - (size / 2);
            int r = (int)(surface.ScriptForegroundColor.R * 0.5f);
            int g = (int)(surface.ScriptForegroundColor.G * 0.5f);
            int b = (int)(surface.ScriptForegroundColor.B * 0.5f);

            float deg = 360 * value;
            float flip = value < 0.5f ? 1 : -1;

            // Fundo
            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = position,
                Size = size,
                Color = deg > 358 && turnDarkOnComplete ? surface.ScriptForegroundColor : new Color(r, g, b),
                Alignment = TextAlignment.LEFT
            });

            if (deg > 358) return;

            float val = value < 0.5f ? 180 : 0;

            // Cobertura 1
            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SemiCircle",
                Position = position,
                Size = size,
                Color = surface.ScriptForegroundColor,
                RotationOrScale = MathHelper.ToRadians((flip * 90) + deg - val),
                Alignment = TextAlignment.LEFT
            });

            // Cobertura 2
            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SemiCircle",
                Position = position,
                Size = size,
                Color = value > 0.5f ? surface.ScriptForegroundColor : new Color(r, g, b),
                RotationOrScale = MathHelper.ToRadians(flip * (-90)),
                Alignment = TextAlignment.LEFT
            });
        }

        private void DrawTitle(float value, bool turnDarkOnComplete)
        {
            Vector2 titleSize = new Vector2(size.X, 18);
            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareSimple",
                Position = origo - new Vector2(size.X, size.Y + (titleSize.Y / 2) + 10),
                Size = new Vector2(size.X * 2, titleSize.Y),
                Color = new Color(0, 0, 0, 140),
                Alignment = TextAlignment.LEFT
            });

            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = title + " (" + (value * 100).ToString("F0") + "%)",
                Position = origo - new Vector2(size.X - 4, size.Y + (titleSize.Y / 2) + 16),
                Color = surface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                RotationOrScale = 0.55f
            });
        }
    }
}
