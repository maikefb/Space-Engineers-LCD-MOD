using VRageMath;

namespace Graph.Extensions
{
    public static class ColorExtensions
    {
        public static string ToHex(this Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        
        public static string ToHex(this Color? color)
        {
            if(color == null)
                color = Color.White;
            return ToHex(color.Value);
        }
        
        public static Color Invert(this Color color)
        { 
            return new Color(255 - color.R, 255 - color.G, 255 - color.B);
        }
        
        public static Color Invert(this Color? color)
        {
            if(color == null)
                color = Color.White;
            return Invert(color.Value);
        }
        
        public static Color DeriveAscentColor(this Color @base)
        {
            var hsv = @base.ColorToHSV();

            if (hsv.Y > 0.3f)
                hsv.Y = hsv.Y > 0.7f ? hsv.Y - 0.3f : hsv.Y + 0.3f;
            else
                hsv.Z = hsv.Z > 0.5f ? hsv.Z - 0.5f : hsv.Z + 0.5f;

            hsv.Y = MathHelper.Clamp(hsv.Y, 0f, 1f);
            hsv.Z = MathHelper.Clamp(hsv.Z, 0f, 1f);

            var color = hsv.HSVtoColor();
            color.A = @base.A;
            return color;
        }
    }
}