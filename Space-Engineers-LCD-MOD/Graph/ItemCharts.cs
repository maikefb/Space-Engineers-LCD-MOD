using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Graph.Data.Scripts.Graph.Sys;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Graph.Data.Scripts.Graph
{
    public abstract class ItemCharts : ChartBase
    {
        protected ItemCharts(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }
        
        
        protected override void DrawTitle(List<MySprite> frame, float scale, Color color)
        {
            var margin = ViewBox.Size.X * Margin;

            Vector2 position = ViewBox.Position;
            position.X += margin;
            position.Y += (ViewBox.Size.Y * Margin) / 2;

            CaretY = position.Y;

            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Textures\\FactionLogo\\Others\\OtherIcon_5.dds",
                Position = position + new Vector2(10f, 20) * scale,
                Size = new Vector2(40 * scale),
                Color = color,
                Alignment = TextAlignment.CENTER
            });
            position.X += ViewBox.Width / 8f;
            frame.Add(MySprite.CreateClipRect(new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + (ViewBox.X) - 105 * scale),
                (int)(position.Y + 35 * scale))));
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = MyTexts.GetString(Title),
                Position = position,
                RotationOrScale = scale * 1.3f,
                Color = color,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });
            frame.Add(MySprite.CreateClearClipRect());
            position.X = ViewBox.Width + ViewBox.X - margin;
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = MyTexts.GetString("BlockPropertyTitle_Stockpile"),
                Position = position,
                RotationOrScale = scale * 1.3f,
                Color = color,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            CaretY += 40 * scale;
        }
    }
}