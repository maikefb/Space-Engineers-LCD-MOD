using System.Linq;
using System.Text;
using Graph.Data.Scripts.Graph;
using Graph.Data.Scripts.Graph.Sys;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;
using VRageMath;

namespace Space_Engineers_LCD_MOD.Controls
{
    public sealed class QuotaButton : TerminalControlsWrapper
    {
        public override IMyTerminalControl TerminalControl { get; }

        public QuotaButton()
        {
            var slider = CreateControl<IMyTerminalControlButton>("QuotaButton");
            slider.Action = Action;
            slider.Visible = Visible;
            slider.Title = MyStringId.GetOrCompute("Department_AssistantProducer");

            TerminalControl = slider;
        }

        public override bool Visible(IMyTerminalBlock block)
        {
            return block is IMyAssembler && TerminalSessionComponent.ShowTextPanelAction != null;
        }

        void Action(IMyTerminalBlock obj)
        {
            if (GridLogicSession.PendingTextAction != null)
                return;

            GridLogicSession.PendingTextAction = grid => PendingTextAction(obj, grid);


            MatrixD mat = MyAPIGateway.Session.Player.Character.WorldMatrix;
            var pos = MyAPIGateway.Entities.FindFreePlace(MyAPIGateway.Session.Player.GetPosition(), 2);

            if (pos == null)
                MyAPIGateway.Utilities.SendMessage("Fail to generate text box");
            else
                MyVisualScriptLogicProvider.SpawnPrefab("Space_Engineers_LCD_MOD_FakeGrid", pos.Value,
                    mat.Forward, mat.Up);
        }

        bool _isActive;
        bool _isActivating;

        void PendingTextAction(IMyTerminalBlock target, IMyCubeGrid grid)
        {
            StringBuilder sb = new StringBuilder();

            if (target is IMyAssembler)
            {
                var assembler = (IMyAssembler)target;
                var blueprints = MyDefinitionManager.Static.GetBlueprintDefinitions();

                MyIni ini = new MyIni();

                foreach (var bp in blueprints)
                {
                    if (!assembler.CanUseBlueprint(bp))
                        continue;

                    var item = MyDefinitionManager.Static.TryGetPhysicalItemDefinition(bp.Results.First().Id);
                    if (item == null)
                        continue;

                    var id = bp.Results.First().Id.ToString();
                    MyAPIGateway.Utilities.ShowNotification(id, 5000);
                    var name = item.DisplayNameEnum;
                    if (name != null)
                    {
                        ini.Set("Quota", id.Substring(16), 0);
                        ini.SetComment("Quota", id.Substring(16), "\n"+MyTexts.GetString(name.Value));
                    }
                }

                sb.Append(ini.ToString().Replace(";\n", "\n").Replace("]\n", "]"));;
            }

            _timeoutTicks = 300;

            GridLogicSession.ActiveAction = () => CurrentAction(target, grid, sb);
            GridLogicSession.PendingTextAction = null;
        }

        int _timeoutTicks;

        void CurrentAction(IMyTerminalBlock target, IMyCubeGrid grid, StringBuilder text)
        {
            _timeoutTicks--;
    
            if (_timeoutTicks == 0) 
                Clear(grid);
            
            var zero = grid.GetCubeBlock(Vector3I.Zero);
            var panel = zero?.FatBlock as IMyTextPanel;
            if (panel != null)
            {
                StringBuilder sb = new StringBuilder();
                panel.ReadText(sb);

                if (_isActivating)
                {
                    if (sb.Length == 0)
                        return;

                    _isActivating = false;
                    _isActive = true;
                    TerminalSessionComponent.ShowTextPanelAction.Invoke(panel);
                }


                if (panel.WritePublicTitle(target.CustomName))
                {
                    if (_isActive)
                    {
                        MyAPIGateway.Utilities.ShowNotification(sb.ToString());
                        GridLogicSession.ActiveAction = null;
                        _isActive = false;
                        Clear(grid);
                    }
                    else
                    {
                        panel.WriteText(text, true);
                        _isActivating = true;
                    }
                }
            }
            else
            {
                GridLogicSession.PendingTextAction = null;
            }
        }

        private void Clear(IMyCubeGrid target)
        {
            GridLogicSession.ActiveAction = null;
            _isActivating = false;
            _isActive = false;
            target.Close();
        }
    }
}