using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Graph.Panels
{
    public class BarPanel
    {
        const float EPSILON = 0.0001f;

        public enum Style
        {
            PillBleed,
            Ellipse
        }

        Color _bgColor;
        Color _fillColor;
        Vector2 _position;
        float _radius;
        Vector2 _size;
        Style _style;
        readonly List<MySprite> _sprites = new List<MySprite>(6);

        bool _layoutDirty = true;
        bool _hasCachedState;
        float _cachedFraction;
        Color _cachedRenderFillColor;

        public BarPanel(
            Vector2 posTopLeft,
            Vector2 size,
            Color fillColor,
            Color bgColor,
            float cornerRadius = -1f,
            Style style = Style.PillBleed)
        {
            SetLayout(posTopLeft, size, fillColor, bgColor, cornerRadius, style);
        }

        public void SetLayout(
            Vector2 posTopLeft,
            Vector2 size,
            Color fillColor,
            Color bgColor,
            float cornerRadius = -1f,
            Style style = Style.PillBleed)
        {
            var normalizedSize = new Vector2(MathHelper.Max(1f, size.X), MathHelper.Max(1f, size.Y));
            var normalizedPosition = new Vector2(posTopLeft.X, posTopLeft.Y + (normalizedSize.Y / 2f));
            var maxR = normalizedSize.Y * 0.5f;
            var radius = cornerRadius > 0f ? MathHelper.Min(cornerRadius, maxR) : maxR;

            if (_position == normalizedPosition &&
                _size == normalizedSize &&
                _fillColor == fillColor &&
                _bgColor == bgColor &&
                Math.Abs(_radius - radius) <= EPSILON &&
                _style == style)
            {
                return;
            }

            _position = normalizedPosition;
            _size = normalizedSize;
            _fillColor = fillColor;
            _bgColor = bgColor;
            _radius = radius;
            _style = style;
            _layoutDirty = true;
        }

        public List<MySprite> GetSprites(float fraction, Color? fillColorOverride = null)
        {

            var f = fraction > .99f ? 1 : MathHelper.Clamp(fraction, 0f, 1f);
            var renderFillColor = fillColorOverride ?? _fillColor;
            if (!_layoutDirty &&
                _hasCachedState &&
                Math.Abs(_cachedFraction - f) <= EPSILON &&
                _cachedRenderFillColor == renderFillColor)
            {
                return _sprites;
            }

            _sprites.Clear();

            if (_style == Style.Ellipse)
            {
                if (f < 1f)
                    _sprites.Add(MakeTex("Circle", _position, _size, _bgColor));
                if (f > 0f)
                {
                    var w = _size.X * f;
                    _sprites.Add(MakeTex("Circle", _position, new Vector2(w, _size.Y), renderFillColor));
                }

                _cachedFraction = f;
                _cachedRenderFillColor = renderFillColor;
                _layoutDirty = false;
                _hasCachedState = true;
                return _sprites;
            }

            if (f < 1f)
                AddPill(_size.X, _bgColor);

            var fillW = _size.X * f;
            if (fillW > 0.001f)
                AddPill(fillW + 1f, renderFillColor, -1f);

            _cachedFraction = f;
            _cachedRenderFillColor = renderFillColor;
            _layoutDirty = false;
            _hasCachedState = true;
            return _sprites;
        }

        void AddPill(float width, Color color, float xOffset = 0f)
        {
            var w = MathHelper.Clamp(width, 0f, _size.X);
            var h = _size.Y;
            if (w <= 0f || h <= 0f) return;

            var r = _radius;
            var d = r * 2f;
            var bleed = MathHelper.Clamp(h * 0.08f, 1f, 3f);

            if (w <= d + 0.001f)
            {
                _sprites.Add(MakeTex("Circle", _position + new Vector2(xOffset, 0f), new Vector2(w, h), color));
                return;
            }

            _sprites.Add(MakeTex("Circle", _position + new Vector2(xOffset, 0f), new Vector2(d, h), color));
            _sprites.Add(MakeTex("Circle", _position + new Vector2(xOffset + (w - d), 0), new Vector2(d, h), color));

            var rectX = r - bleed;
            var rectW = w - 2f * r + 2f * bleed;
            if (rectW > 0.25f)
                _sprites.Add(MakeTex("SquareSimple", _position + new Vector2(xOffset + rectX, 0f), new Vector2(rectW, h), color));
        }

        static MySprite MakeTex(string name, Vector2 posTopLeft, Vector2 size, Color color)
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
