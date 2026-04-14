using System.Collections.Generic;
using System.Text;
using Graph.Charts;
using Graph.System.Config;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage;
using VRage.ModAPI;
using VRage.Utils;

namespace Graph.System.TerminalControls.Generic
{
    public sealed class ComboboxDisplayMode : TerminalControlsWrapper
    {
        protected override string[] VisibleForScripts { get; } = { InventoryCharts.ID, ProjectorCharts.ID, RenewableGraph.ID, GeneratorsGraph.ID, ContainerGraph.ID };

        public override IMyTerminalControl TerminalControl { get; }

        public ComboboxDisplayMode()
        {
            var slider = CreateControl<IMyTerminalControlCombobox>("ComboboxTable");
            slider.Getter = Getter;
            slider.ComboBoxContent = Content;
            slider.Setter = Setter;
            slider.Visible = Visible;
            slider.Title = MyStringId.GetOrCompute("DisplayName_Item_Display");

            TerminalControl = slider;
        }

        void Content(List<MyTerminalControlComboBoxItem> obj)
        {
            obj.Add(new MyTerminalControlComboBoxItem
            {
                Key = 0,
                Value = MyStringId.GetOrCompute("GamepadScheme_Default")
            });
            
            obj.Add(new MyTerminalControlComboBoxItem
            {
                Key = 1,
                Value = MyStringId.GetOrCompute("LCD_Grid")
            });
        }

        void Setter(IMyTerminalBlock block, long l)
        {
            var config = ConfigManager.GetConfigForCurrentScreen(block);
            if (config == null)
                return;

            config.DisplayInternal = (int)l;
            ConfigManager.Sync(block);
        }

        long Getter(IMyTerminalBlock block)
        {
            var config = ConfigManager.GetConfigForCurrentScreen(block);
            if (config == null)
                return 1;

            return config.DisplayInternal;
        }
    }
}
