using Graph.Data.Scripts.Graph;
using Graph.Data.Scripts.Graph.Sys;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;
using VRage.Utils;
using VRageMath;

namespace Space_Engineers_LCD_MOD.Controls.Generic
{
    /// <summary>
    /// Color picker for Header for many Scripts using <see cref="ScreenConfig"/> 
    /// </summary>
    public sealed class ColorPickerHeader : TerminalControlsWrapper
    {
        public override IMyTerminalControl TerminalControl { get; }

        public ColorPickerHeader()
        {
            var colorPicker = CreateControl<IMyTerminalControlColor>("HeaderColor");
            colorPicker.Getter = Getter;
            colorPicker.Setter = Setter;
            colorPicker.Visible = Visible;
            colorPicker.Title = MyStringId.GetOrCompute("BlockPropertyTitle_TextPanelPublicTitle");
            TerminalControl = colorPicker;
        }

        void Setter(IMyTerminalBlock block, Color color)
        {
            var index = GetThisSurfaceIndex(block);
            if (index == -1) return;
            MyTuple<int, ScreenProviderConfig> settings;
            if (ChartBase.ActiveScreens.TryGetValue(block, out settings) && settings.Item2.Screens.Count > index)
            {
                settings.Item2.Screens[index].HeaderColor = color;
                settings.Item2.Dirty = true;
            }
        }

        Color Getter(IMyTerminalBlock block)
        {
            var index = GetThisSurfaceIndex(block);

            MyTuple<int, ScreenProviderConfig> settings;
            if (ChartBase.ActiveScreens.TryGetValue(block, out settings) && settings.Item2.Screens.Count > index)
                return settings.Item2.Screens[index].HeaderColor;


            return Color.White;
        }
    }
}