using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Controls.Filter.Listbox;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Controls.Filter.Buttons
{
    public abstract class TerminalControlFilterButton : TerminalControlFilter
    {
        protected readonly TerminalControlsListbox SourceList;
        protected readonly TerminalControlsListbox TargetList;
        
        public override IMyTerminalControl TerminalControl => _terminalControl;
        IMyTerminalControl _terminalControl;
        
        protected TerminalControlFilterButton(TerminalControlsListbox sourceList, TerminalControlsListbox targetList)
        {
            SourceList = sourceList;
            TargetList = targetList;
        }

        protected void CreateButton(string id, string title)
        {
            var button = CreateControl<IMyTerminalControlButton>(id);
            button.Action = Action;
            button.Visible = Visible;
            button.Title = MyStringId.GetOrCompute(title);
            _terminalControl = button;
        }

        protected abstract void Action(IMyTerminalBlock block);
    }
}