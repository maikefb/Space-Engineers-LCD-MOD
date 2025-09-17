using System.Collections.Generic;
using Graph.Data.Scripts.Graph.Sys;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace Graph.Data.Scripts.Graph
{
    public abstract class ItemCharts : MyTextSurfaceScriptBase
    {
        protected GridLogic GridLogic = null;
        
        protected ItemCharts(Sandbox.ModAPI.Ingame.IMyTextSurface surface, VRage.Game.ModAPI.Ingame.IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        public abstract Dictionary<string, double> ItemSource { get; }

        public override void Run()
        {
            if(GridLogic == null)
                GridLogicSession.components.TryGetValue(Block.CubeGrid.EntityId, out GridLogic);

            base.Run();
        }
    }
}