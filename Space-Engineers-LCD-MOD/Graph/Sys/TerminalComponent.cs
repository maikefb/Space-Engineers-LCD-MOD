using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Controls;
using Space_Engineers_LCD_MOD.Graph.Config;
using Space_Engineers_LCD_MOD.Helpers;
using Space_Engineers_LCD_MOD.Networking;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Utils;
using VRageMath;


namespace Graph.Data.Scripts.Graph.Sys
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class TerminalSessionComponent : MySessionComponentBase
    {
        MyEasyNetworkManager _networkManager;

        public override void BeforeStart()
        {
#if DEBUG
            try
            {
                throw new Exception("Hello DNSpy");
            }
            catch
            {
                /* workaround for Debugger.Attach() not available for Mods */
            }
#endif
            try
            {
                _networkManager = new MyEasyNetworkManager(46541);

                _networkManager.OnReceivedPacket += OnReceivedPacket;
                MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;

                var source = new ListboxBlockSelection();
                var target = new ListboxBlockSelected();

                _controls.Add(new ColorPickerHeader());
                _controls.Add(new SeparatorFilter());
                _controls.Add(new LabelSeparator());
                _controls.Add(source);
                _controls.Add(new ButtonAddToSelection(source, target));
                _controls.Add(target);
                _controls.Add(new ButtonRemoveFromSelection(source, target));
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        readonly List<TerminalControlsCharts> _controls = new List<TerminalControlsCharts>();

        void OnReceivedPacket(MyEasyNetworkManager.PacketIn packetRaw)
        {
            try
            {
                if (packetRaw.PacketId == 1)
                {
                    var packet = packetRaw.UnWrap<PacketSyncScreenConfig>();
                    var block = MyEntities.GetEntityById(packet.BlockId) as IMyFunctionalBlock;

                    if (block == null)
                        return;

                    MyTuple<int, ScreenProviderConfig> settings;
                    if (ChartBase.ActiveScreens.TryGetValue(block, out settings))
                    {
                        if (settings.Item2.Screens.Count != packet.Config.Screens.Count)
                            return;

                        settings.Item2.Dirty = false;
                        for (var index = 0; index < settings.Item2.Screens.Count; index++)
                            settings.Item2.Screens[index].CopyFrom(packet.Config.Screens[index]);

                        ChartBase.Save(block, settings.Item2);
                    }
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.TerminalControls.CustomControlGetter -= CustomControlGetter;
            foreach (var blockPair in ChartBase.ActiveScreens) 
                ChartBase.Save(blockPair.Key, blockPair.Value.Item2);
            
            ChartBase.ActiveScreens.Clear();
            ChartBase.ActiveScreens = null;
            _controls.Clear();
        }

        public override void SaveData()
        {
            try
            {
                foreach (var screen in ChartBase.ActiveScreens)
                    ChartBase.Save(screen.Key, screen.Value.Item2);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }

            base.SaveData();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            try
            {
                foreach (var screen in ChartBase.ActiveScreens)
                {
                    if (!screen.Value.Item2.Dirty)
                        return;

                    _networkManager.TransmitToServer(
                        new PacketSyncScreenConfig(screen.Key.EntityId, screen.Value.Item2));
                    screen.Value.Item2.Dirty = false;

                    ChartBase.Save(screen.Key, screen.Value.Item2);
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        void CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (controls == null)
                return;

            try
            {
                SetupProviderTerminal(block, controls);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        void SetupProviderTerminal(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            var provider = block as IMyTextSurfaceProvider;

            if (provider == null)
                return;

            if (block is IMyTextPanel)
            {
                foreach (var control in _controls)
                {
                    controls.Add(control.TerminalControl);
                }
            }
            else if (provider.SurfaceCount > 0)
            {
                var index = controls.FindIndex(p => p.Id == "Script") + 3;

                foreach (var control in _controls)
                {
                    controls.AddOrInsert(control.TerminalControl, index);
                    index++;
                }
            }
        }
    }
}