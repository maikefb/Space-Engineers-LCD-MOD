/*
 * ExampleWorkaround_GridLogicHook.cs
 * https://github.com/THDigi/SE-ModScript-Examples/blob/master/Data/Scripts/Examples/ExampleWorkaround_GridLogicHook.cs
 * Workaround to Grid-Logic Client side
 * By THDigi
 */

using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Helpers;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Graph.Data.Scripts.Graph.Sys
{
    // This shows how to find all grids and execute code on them as if it was a gamelogic component.
    // This is needed because attaching gamelogic to grids does not work reliably, like not working at all for clients in MP.
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class GridLogicSession : MySessionComponentBase
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

            MyAPIGateway.Entities.OnEntityAdd += EntityAdded;
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
        }

        private void EntityAdded(IMyEntity ent)
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
                    if(PendingTextAction != null)
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

        private void GridMarkedForClose(IMyEntity ent)
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