/*
 * ExampleWorkaround_GridLogicHook.cs
 * https://github.com/THDigi/SE-ModScript-Examples/blob/master/Data/Scripts/Examples/ExampleWorkaround_GridLogicHook.cs
 * Workaround to Grid-Logic Client side
 * By THDigi
 */

using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Graph.Data.Scripts.Graph.Sys
{
    // This shows how to find all grids and execute code on them as if it was a gamelogic component.
    // This is needed because attaching gamelogic to grids does not work reliably, like not working at all for clients in MP.
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class GridLogicSession : MySessionComponentBase
    {
        readonly Dictionary<long, MyTuple<IMyCubeGrid, GridLogic>> _grids = new Dictionary<long, MyTuple<IMyCubeGrid, GridLogic>>();
        public static Dictionary<long, GridLogic> components = new Dictionary<long, GridLogic>();

        public override void LoadData()
        {
            if(MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                return;
            
            MyAPIGateway.Entities.OnEntityAdd += EntityAdded;
        }

        protected override void UnloadData()
        {
            if(MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                return;
            
            MyAPIGateway.Entities.OnEntityAdd -= EntityAdded;
            _grids.Clear();
            components.Clear();
            components = null;
        }

        private void EntityAdded(IMyEntity ent)
        {
            var grid = ent as IMyCubeGrid;

            if(grid != null)
            {
                var logic = new GridLogic(grid);
                _grids.Add(grid.EntityId, new MyTuple<IMyCubeGrid, GridLogic>(grid, logic));
                components.Add(grid.EntityId, logic);
                grid.OnMarkForClose += GridMarkedForClose;
            }
        }

        private void GridMarkedForClose(IMyEntity ent)
        {
            _grids.Remove(ent.EntityId);
            components.Remove(ent.EntityId);
        }

        public override void UpdateBeforeSimulation()
        {
            if(MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                return;
            
            try
            {
                foreach(var grid in _grids.Values)
                {
                    if(grid.Item1.MarkedForClose)
                        continue;

                    grid.Item2.Update();
                }
            }
            catch(Exception e)
            {
                MyLog.Default.WriteLineAndConsole(e.ToString());

                if(MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
            }
        }
    }
}