using Graph.System.Config;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using VRageMath;

namespace Graph.System.TerminalControls.Generic
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
            var config = ConfigManager.GetConfigForCurrentScreen(block);
            if(config == null)
                return;
            config.HeaderColor = color;
            ConfigManager.Sync(block);
        }

        Color Getter(IMyTerminalBlock block)
        {
            var config = ConfigManager.GetConfigForCurrentScreen(block);
            if (config?.HeaderColor != null)
                return config.HeaderColor;
            
            return Color.White;
        }
    }
}