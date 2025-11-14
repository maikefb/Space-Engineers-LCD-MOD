using VRage.Game.GUI.TextPanel;
using Color = VRageMath.Color;

namespace Graph.Extensions
{
    public static class MySpriteExtensions
    {
        public static MySprite Shadow(this MySprite sprite, float offset, Color? color = null)
        {
            if (color == null) color = sprite.Color.Invert();
            return new MySprite
            {
                Type = sprite.Type,
                Data = sprite.Data,
                Position = sprite.Position + offset,
                RotationOrScale = sprite.RotationOrScale,
                Color = color,
                Alignment = sprite.Alignment,
                FontId = sprite.FontId,
            };
        }
    }
}