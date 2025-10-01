using System;
using System.Collections.Generic;
using System.Linq;
using Graph.Charts;
using Graph.Helpers;
using Graph.Networking;
using Graph.System.Config;
using Graph.System.TerminalControls;
using Graph.System.TerminalControls.Blueprint;
using Graph.System.TerminalControls.Filter;
using Graph.System.TerminalControls.Filter.Buttons;
using Graph.System.TerminalControls.Filter.Listbox;
using Graph.System.TerminalControls.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Graph.System
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class LcdModSessionComponent : MySessionComponentBase
    {
        readonly Dictionary<long, MyTuple<IMyCubeGrid, GridLogic>> _grids =
            new Dictionary<long, MyTuple<IMyCubeGrid, GridLogic>>();

        public static Dictionary<long, GridLogic> Components = new Dictionary<long, GridLogic>();
        public static List<TerminalControlsWrapper> Controls = new List<TerminalControlsWrapper>();

        public override void LoadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                return;

            DebuggerHelper.Break();
            MyAPIGateway.Entities.OnEntityAdd += EntityAdded;

            // lots of faction event to catch everything
            MyAPIGateway.Session.Factions.FactionCreated += FactionUpdated; // user created faction
            MyAPIGateway.Session.Factions.FactionEdited += FactionUpdated; // user changed anything on the faction
            MyAPIGateway.Session.Factions.FactionStateChanged +=
                FactionStateChanged; // user left the faction (and many others)
        }

        void FactionStateChanged(MyFactionStateChange change, long faction1, long faction2, long player, long client)
        {
            if (change < MyFactionStateChange.FactionMemberSendJoin)
                return;

            FactionUpdated(faction1);
            FactionUpdated(faction2);
        }

        void FactionUpdated(long obj)
        {
            var affected = ChartBase.Instances.Where(a => a.Faction != null && a.Faction.FactionId == obj)
                .ToList();

            var faction = MyAPIGateway.Session.Factions.TryGetFactionById(obj);

            if (faction != null)
                affected.AddRange(
                    ChartBase.Instances.Where(a => a.Block != null &&
                                                   (faction.FounderId == a.Block.OwnerId ||
                                                    faction.Members.ContainsKey(a.Block.OwnerId))));

            affected.ForEach(a => a.UpdateFaction(faction));
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                return;

            MyAPIGateway.TerminalControls.CustomControlGetter -= CustomControlGetter;
            Controls.Clear();
            MyAPIGateway.Entities.OnEntityAdd -= EntityAdded;
            _grids.Clear();
            Components.Clear();
            Components = null;

            ItemCharts.SpriteCache?.Clear();
            ItemCharts.SpriteCache = null;

            ListBoxItemHelper.PerTypeCache.Clear();

            ConfigManager.Close();
        }

        void EntityAdded(IMyEntity ent)
        {
            try
            {
                var grid = ent as IMyCubeGrid;
                if (grid == null || _grids.ContainsKey(grid.EntityId))
                    return;

                var logic = new GridLogic(grid);
                _grids[grid.EntityId] = new MyTuple<IMyCubeGrid, GridLogic>(grid, logic);
                Components[grid.EntityId] = logic;
                grid.OnMarkForClose += GridMarkedForClose;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        void GridMarkedForClose(IMyEntity ent)
        {
            try
            {
                _grids.Remove(ent.EntityId);
                Components.Remove(ent.EntityId);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        public override void UpdateBeforeSimulation()
        {
            if (MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                return;

            try
            {
                foreach (var grid in _grids.Values)
                {
                    if (grid.Item1.MarkedForClose)
                        continue;

                    grid.Item2.Update();
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
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

                Controls.Add(new ColorPickerHeader());
                Controls.Add(new SliderChartScale());

                Controls.Add(new SwitchToggleLines());

                Controls.Add(new ListboxProjectorSelection());

                Controls.Add(new SeparatorFilter());
                Controls.Add(new LabelSeparator());
                Controls.Add(source);
                Controls.Add(new ButtonBlockAddToSelection(source, target));
                Controls.Add(target);
                Controls.Add(new ButtonBlockRemoveFromSelection(source, target));

                source = new ListboxItemsCandidates();
                target = new ListboxItemsSelected();

                Controls.Add(target);
                Controls.Add(new ButtonItemRemoveFromSelection(source, target));
                Controls.Add(source);
                Controls.Add(new ButtonItemAddToSelection(source, target));

                Controls.Add(new ComboboxSorting());
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        void OnReceivedPacket(ReceivedPacketEventArgs args)
        {
            try
            {
                if (args.PacketId == 1)
                {
                    var packet = args.UnWrap<NetworkPackageSyncScreenConfig>();
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
                controls.AddRange(Controls.Select(control => control.TerminalControl));
            }
            else if (provider.SurfaceCount > 0)
            {
                var index = controls.FindIndex(p => p.Id == "Script") + 3;

                foreach (var control in Controls)
                {
                    controls.AddOrInsert(control.TerminalControl, index);
                    index++;
                }
            }
        }
    }
}