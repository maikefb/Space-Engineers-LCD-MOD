using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
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
using VRage.Game.Components;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Graph.Sys
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class TerminalSessionComponent : MySessionComponentBase
    {
        public override void UpdatingStopped()
        {
            SaveData();
            base.UpdatingStopped();
        }

        public override void BeforeStart()
        {
            try
            {
                ConfigManager.Init();
                ConfigManager.NetworkManager.OnReceivedPacket += OnReceivedPacket;

                if (MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                    return;

                MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;

                TerminalControlsListbox source = new ListboxBlockCandidates();
                TerminalControlsListbox target = new ListboxBlockSelected();

                _controls.Add(new ColorPickerHeader());
                _controls.Add(new SliderChartScale());

                _controls.Add(new SwitchToggleLines());
                
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
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        readonly List<TerminalControlsWrapper> _controls = new List<TerminalControlsWrapper>();

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

                    ScreenProviderConfig settings;
                    if (MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                    {
                        settings = ConfigManager.TryLoad(block) ?? ConfigManager.CreateSettings(block); 
                        // Server doesn't need to keep track of the setting,
                        // only save/load it from blocks
                    }
                    else
                    {
                        settings = ChartBase.Instances.FirstOrDefault(a => a.Block.Equals(block))?.ProviderConfig;
                    }

                    if (settings == null)
                        return;

                    settings.CopyFrom(packet.Config);
                    ConfigManager.Save(block, settings);
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
            _controls.Clear();
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

            if (provider is IMyTextPanel)
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