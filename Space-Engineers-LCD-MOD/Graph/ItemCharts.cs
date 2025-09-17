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
        /// <summary>
        /// Relative area of the <see cref="Sandbox.ModAPI.IMyTextSurface.TextureSize"/> That is Visible
        /// </summary>
        public readonly RectangleF ViewBox;
        
        protected GridLogic GridLogic;
        
        protected ItemCharts(Sandbox.ModAPI.Ingame.IMyTextSurface surface, VRage.Game.ModAPI.Ingame.IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            var sizeOffset = (surface.TextureSize - surface.SurfaceSize) / 2;
            ViewBox = new RectangleF(sizeOffset.X, sizeOffset.Y, surface.SurfaceSize.X, surface.SurfaceSize.Y);
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