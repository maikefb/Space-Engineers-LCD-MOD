using System;
using System.Globalization;

namespace Graph.Helpers
{
    internal static class FormatingHelper
    {
        public static string DistanceToString(float meters)
        {
            var distance = (double)meters;
            var abs = Math.Abs(distance);
            var sign = distance < 0d ? "-" : "";

            if (abs >= 299792458d)
                return sign + (abs / 299792458d).ToString("0.##", CultureInfo.CurrentUICulture) + " ls";
            if (abs >= 1000000000d)
                return sign + (abs / 1000000000d).ToString("0.##", CultureInfo.CurrentUICulture) + " Gm";
            if (abs >= 1000000d)
                return sign + (abs / 1000000d).ToString("0.##", CultureInfo.CurrentUICulture) + " Mm";
            if (abs >= 1000d)
                return sign + (abs / 1000d).ToString("0.##", CultureInfo.CurrentUICulture) + " km";
            if (abs >= 1d)
                return sign + abs.ToString("0.##", CultureInfo.CurrentUICulture) + " m";

            return sign + (abs * 100d).ToString("0.##", CultureInfo.CurrentUICulture) + " cm";
        }
    }
}
