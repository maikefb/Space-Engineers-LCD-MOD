using System.Globalization;
using VRage;
using VRage.Utils;

namespace Graph.Helpers
{
    public class LocHelper
    {
        public static string Empty => $"- {MyTexts.GetString("BlockPropertyProperties_WaterLevel_Empty")} -";

        public static string Damaged
        {
            get
            {
                var s = MyTexts.Get(MyStringId.GetOrCompute("Damaged")).ToString();

                if (string.IsNullOrEmpty(s))
                    return s;

                return char.ToUpper(s[0], CultureInfo.CurrentCulture) + s.Substring(1);
            }
        }

        public static string GetLoc(string key)
        {
            if (key == "Damaged")
                return Damaged;
            
            return MyTexts.Get(MyStringId.GetOrCompute(key)).ToString();
        }
    }
}