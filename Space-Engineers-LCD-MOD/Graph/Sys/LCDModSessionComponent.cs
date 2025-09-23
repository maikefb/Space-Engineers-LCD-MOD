using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Graph.Config;
using Space_Engineers_LCD_MOD.Helpers;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Space_Engineers_LCD_MOD.Graph.Sys
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class LcdModSessionComponent : MySessionComponentBase
    {
        readonly Dictionary<long, MyTuple<IMyCubeGrid, GridLogic>> _grids =
            new Dictionary<long, MyTuple<IMyCubeGrid, GridLogic>>();

        public static Dictionary<long, GridLogic> Components = new Dictionary<long, GridLogic>();

        public static Action<IMyCubeGrid> PendingTextAction;
        public static Action ActiveAction;

        public override void LoadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                return;

            DebuggerHelper.Break();
            MyAPIGateway.Entities.OnEntityAdd += EntityAdded;
            
            // lots of faction event to catch everything
            MyAPIGateway.Session.Factions.FactionCreated += FactionUpdated; // user created faction
            MyAPIGateway.Session.Factions.FactionEdited += FactionUpdated; // user changed anything on the faction
            MyAPIGateway.Session.Factions.FactionStateChanged += FactionStateChanged; // user left the faction (and many others)
        }

        void FactionStateChanged(MyFactionStateChange arg1, long arg2, long arg3, long arg4, long arg5)
        {
            if (arg1 < MyFactionStateChange.FactionMemberSendJoin) 
                return;

            FactionUpdated(arg2);
            FactionUpdated(arg3);
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

            MyAPIGateway.Entities.OnEntityAdd -= EntityAdded;
            _grids.Clear();
            Components.Clear();
            Components = null;

            ItemCharts.SpriteCache?.Clear();
            ItemCharts.SpriteCache = null;
            ConfigManager.Close();
        }

        void EntityAdded(IMyEntity ent)
        {
            try
            {
                var grid = ent as IMyCubeGrid;
                if (grid == null || _grids.ContainsKey(grid.EntityId))
                    return;

                if (grid.CustomName == "Space_Engineers_LCD_MOD_FakeGrid")
                {
                    grid.Visible = false;
                    grid.Physics = null;
                    if (PendingTextAction != null)
                        PendingTextAction.Invoke(grid);
                    return;
                }

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

                if (ActiveAction != null)
                {
                    ActiveAction.Invoke();
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }
    }
}