using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Graph.Panels
{
    public class PieDualChartPanel : PieChartPanel
    {
        bool _hasCachedSecondState;
        float _cachedValue2;

        public PieDualChartPanel(string title, IMyTextSurface surface, Vector2 margin, Vector2 size,
            bool showTitle = true) : base(title, surface, margin, size, showTitle)
        {
        }

        public List<MySprite> GetSprites(float value, float value2, Color? color = null,
            bool turnDarkOnComplete = false)
        {
            if (color == null)
                color = _surface.ScriptForegroundColor;

            var backgroundColor = _surface.ScriptForegroundColor;
            if (!_layoutDirty &&
                _hasCachedState &&
                _hasCachedSecondState &&
                Math.Abs(_cachedValue - value) <= EPSILON &&
                Math.Abs(_cachedValue2 - value2) <= EPSILON &&
                _cachedColor == color.Value &&
                _cachedTurnDarkOnComplete == turnDarkOnComplete &&
                _cachedBackgroundColor == backgroundColor)
            {
                return _sprites;
            }

            _sprites.Clear();

            if (_showTitle) DrawTitle(value, color.Value);
            DrawBackground(Math.Max(value, value2),
                (value > value2) ? backgroundColor : color.Value, backgroundColor, turnDarkOnComplete);

            if(value > 0 && value > value2) // draw only if > 0 and bigger than the second value
                DrawPie(value, backgroundColor, backgroundColor);
            
            if(value2 > 0 && value2 < .99)  // draw only if > 0 and not 100% (turnDarkOnComplete already draws 100%)
                DrawPieWithTransparency(value2, color.Value);

            _cachedValue = value;
            _cachedValue2 = value2;
            _cachedColor = color.Value;
            _cachedTurnDarkOnComplete = turnDarkOnComplete;
            _cachedBackgroundColor = backgroundColor;
            _layoutDirty = false;
            _hasCachedState = true;
            _hasCachedSecondState = true;

            return _sprites;
        }

        protected virtual void DrawPieWithTransparency(float value, Color color)
        {
            Vector2 position = _origo - (_size / 2);

            float deg = 360 * value;
            float flip = value < 0.5f ? 1 : -1;

            if (value > .99)
                return;

            float val = value < 0.5f ? 180 : 0;

            // Cover 1
            var semiCircle = new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SemiCircle",
                Position = position,
                Size = _size,
                Color = color,
                RotationOrScale = MathHelper.ToRadians((flip * 90) + deg - val),
                Alignment = TextAlignment.LEFT
            };

            if (value < 0.5f)
            {
                _sprites.Add(new MySprite(SpriteType.CLIP_RECT, null,
                    new Vector2(position.X + _size.X / 2, position.Y - _size.Y / 2),
                    _size // the X is bigger, but we don't care about width
                ));
                _sprites.Add(semiCircle);
                _sprites.Add(MySprite.CreateClearClipRect());
            }
            else
            {
                _sprites.Add(semiCircle);
            }

            if (value <= 0.5f)
                return;

            // Cover 2
            _sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SemiCircle",
                Position = position,
                Size = _size,
                Color = color,
                RotationOrScale = MathHelper.ToRadians(flip * (-90)),
                Alignment = TextAlignment.LEFT
            });
        }
    }
}
