using VRage;
using VRage.Utils;

namespace Graph.Helpers
{
    public class LocHelper
    {
        public static string Empty => $"- {MyTexts.GetString("BlockPropertyProperties_WaterLevel_Empty")} -";
        
        public static string GetLoc(string key)
        {
            return MyTexts.Get(MyStringId.GetOrCompute(key)).ToString();
        }
    }
}
