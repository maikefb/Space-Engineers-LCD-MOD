using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Controls;
using Space_Engineers_LCD_MOD.Controls.Blueprint;
using Space_Engineers_LCD_MOD.Controls.Filter;
using Space_Engineers_LCD_MOD.Controls.Filter.Buttons;
using Space_Engineers_LCD_MOD.Controls.Filter.Listbox;
using Space_Engineers_LCD_MOD.Controls.Generic;
using Space_Engineers_LCD_MOD.Graph.Config;
using Space_Engineers_LCD_MOD.Helpers;
using Space_Engineers_LCD_MOD.Networking;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;


namespace Graph.Data.Scripts.Graph.Sys
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class TerminalSessionComponent : MySessionComponentBase
    {
        MyEasyNetworkManager _networkManager;

        /// <summary>
        /// Captured Terminal Action for Open Text Edit on the Screen
        /// </summary>
        public static Action<IMyTerminalBlock> ShowTextPanelAction;

        public override void UpdatingStopped()
        {
            SaveData();
            base.UpdatingStopped();
        }

        public override void BeforeStart()
        {
            DebuggerHelper.Break();

            try
            {
                _networkManager = new MyEasyNetworkManager(46541);

                _networkManager.OnReceivedPacket += OnReceivedPacket;
                MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;

                TerminalControlsListbox source = new ListboxBlockCandidates();
                TerminalControlsListbox target = new ListboxBlockSelected();

                _controls.Add(new ColorPickerHeader());
                _controls.Add(new SliderChartScale());

                _controls.Add(new ListboxProjectorSelection());

                _controls.Add(new SeparatorFilter());
                _controls.Add(new LabelSeparator());
                _controls.Add(source);
                _controls.Add(new ButtonBlockAddToSelection(source, target));
                _controls.Add(target);
                _controls.Add(new ButtonBlockRemoveFromSelection(source, target));

                source = new ListboxItemsCandidates();
                target = new ListboxItemsSelected();

                _controls.Add(target);
                _controls.Add(new ButtonItemRemoveFromSelection(source, target));
                _controls.Add(source);
                _controls.Add(new ButtonItemAddToSelection(source, target));

                _assemblerControls.Add(new QuotaButton());
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        readonly List<TerminalControlsWrapper> _controls = new List<TerminalControlsWrapper>();
        readonly List<TerminalControlsWrapper> _assemblerControls = new List<TerminalControlsWrapper>();

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
            _assemblerControls.Clear();
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

            if (ShowTextPanelAction == null)
            {
                var button =
                    controls.FirstOrDefault(a => a is IMyTerminalControlButton && a.Id == "ShowTextPanel") as
                        IMyTerminalControlButton;
                if (button != null)
                    ShowTextPanelAction = button.Action;
            }

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
            if (block is IMyAssembler)
            {
                foreach (var control in _assemblerControls)
                {
                    controls.Add(control.TerminalControl);
                }
            }
            else if (block is IMyTextSurfaceProvider)
            {
                var provider = block as IMyTextSurfaceProvider;
                
                if (block is IMyTextPanel)
                {
                    controls.AddRange(_controls.Select(control => control.TerminalControl));
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
}