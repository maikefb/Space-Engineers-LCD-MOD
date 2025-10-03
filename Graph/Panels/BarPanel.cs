using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Graph.Panels
{
    public class BarPanel
    {
        public enum Style
        {
            PillBleed,
            Ellipse
        }

        private readonly Color _bgColor;
        private readonly Color _fillColor;
        private readonly Vector2 _posTopLeft;
        private readonly float _radius;
        private readonly Vector2 _size;
        private readonly Style _style;

        public BarPanel(
            Vector2 posTopLeft,
            Vector2 size,
            Color fillColor,
            Color bgColor,
            float cornerRadius = -1f,
            Style style = Style.PillBleed)
        {
            _posTopLeft = posTopLeft;
            _size = new Vector2(MathHelper.Max(1f, size.X), MathHelper.Max(1f, size.Y));
            _fillColor = fillColor;
            _bgColor = bgColor;
            _style = style;

            var maxR = _size.Y * 0.5f;
            _radius = cornerRadius > 0f ? MathHelper.Min(cornerRadius, maxR) : maxR;
        }

        public List<MySprite> GetSprites(float fraction, Color? colorOverride = null, bool drawBackground = true)
        {
            var list = new List<MySprite>();
            var f = MathHelper.Clamp(fraction, 0f, 1f);
            var fillCol = colorOverride ?? _fillColor;

            if (_style == Style.Ellipse)
            {
                if (drawBackground)
                    list.Add(MakeTex("Circle", _posTopLeft, _size, _bgColor));
                if (f > 0f)
                {
                    var w = _size.X * f;
                    list.Add(MakeTex("Circle", _posTopLeft, new Vector2(w, _size.Y), fillCol));
                }

                return list;
            }

            if (drawBackground)
                AddPill(ref list, _size.X, _bgColor);

            var fillW = _size.X * f;
            if (fillW > 0.001f)
                AddPill(ref list, fillW, fillCol);

            return list;
        }

        private void AddPill(ref List<MySprite> list, float width, Color color)
        {
            var w = MathHelper.Clamp(width, 0f, _size.X);
            var h = _size.Y;
            if (w <= 0f || h <= 0f) return;

            var r = _radius;
            var d = r * 2f;
            var bleed = MathHelper.Clamp(h * 0.08f, 1f, 3f);

            if (w <= d + 0.001f)
            {
                list.Add(MakeTex("Circle", _posTopLeft, new Vector2(w, h), color));
                return;
            }

            list.Add(MakeTex("Circle", _posTopLeft, new Vector2(d, h), color));
            list.Add(MakeTex("Circle", _posTopLeft + new Vector2(w - d, 0), new Vector2(d, h), color));

            var rectX = r - bleed;
            var rectW = w - 2f * r + 2f * bleed;
            if (rectW > 0.25f)
                list.Add(MakeTex("SquareSimple", _posTopLeft + new Vector2(rectX, 0f), new Vector2(rectW, h), color));
        }

        private static MySprite MakeTex(string name, Vector2 posTopLeft, Vector2 size, Color color)
        {
            return new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = name,
                Position = posTopLeft,
                Size = size,
                Color = color,
                Alignment = TextAlignment.LEFT
            };
        }
    }
}