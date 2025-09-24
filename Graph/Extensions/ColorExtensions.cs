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
    }
}