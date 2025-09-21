using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Graph.Data.Scripts.Graph.Panels
{
    public class BarPanel
    {
        private readonly Color _bgColor;
        private readonly Color _fillColor;
        private readonly Vector2 _posTopLeft;
        private readonly float _radius;
        private readonly Vector2 _size;

        public BarPanel(
            Vector2 posTopLeft,
            Vector2 size,
            Color fillColor,
            Color bgColor,
            float cornerRadius = -1f)
        {
            _posTopLeft = posTopLeft;
            _size = new Vector2(MathHelper.Max(1f, size.X), MathHelper.Max(1f, size.Y));
            _fillColor = fillColor;
            _bgColor = bgColor;

            var maxR = _size.Y * 0.5f;
            _radius = cornerRadius > 0f ? MathHelper.Min(cornerRadius, maxR) : maxR;
        }

        public List<MySprite> GetSprites(float fraction, Color? colorOverride = null, bool drawBackground = true)
        {
            var list = new List<MySprite>();

            var f = fraction;
            if (f < 0f) f = 0f;
            else if (f > 1f) f = 1f;

            var fillCol = colorOverride.HasValue ? colorOverride.Value : _fillColor;

            if (drawBackground)
                AddPill(ref list, _bgColor);

            var fillW = _size.X * f;
            if (fillW > 0.5f)
            {
                list.Add(MySprite.CreateClipRect(new Rectangle(
                    (int)_posTopLeft.X,
                    (int)_posTopLeft.Y,
                    (int)fillW,
                    (int)_size.Y)));

                AddPill(ref list, fillCol);

                list.Add(MySprite.CreateClearClipRect());
            }

            return list;
        }

        private void AddPill(ref List<MySprite> list, Color color)
        {
            var r = _radius;
            var d = r * 2f;

            var rectW = _size.X - d;
            if (rectW > 0.5f)
                list.Add(new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = _posTopLeft + new Vector2(r, 0f),
                    Size = new Vector2(rectW, _size.Y),
                    Color = color,
                    Alignment = TextAlignment.LEFT
                });

            list.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = _posTopLeft,
                Size = new Vector2(d, d),
                Color = color,
                Alignment = TextAlignment.LEFT
            });

            list.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = _posTopLeft + new Vector2(_size.X - d, 0f),
                Size = new Vector2(d, d),
                Color = color,
                Alignment = TextAlignment.LEFT
            });
        }
    }
}