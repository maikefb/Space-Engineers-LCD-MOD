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
    }
}