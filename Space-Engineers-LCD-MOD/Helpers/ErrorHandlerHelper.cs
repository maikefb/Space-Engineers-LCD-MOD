using System;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Helpers
{
    public class ErrorHandlerHelper
    {
        public static void LogError(Exception error, object source)
        {
            MyLog.Default.WriteLineAndConsole(error.ToString());
            if (MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification(
                    $"[ ERROR: {source.GetType().FullName}: {error.Message} | Send SpaceEngineers.Log to mod author ]", 10000,
                    MyFontEnum.Red);
        }
    }
}