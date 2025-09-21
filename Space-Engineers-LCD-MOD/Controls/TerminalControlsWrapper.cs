using System;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.GUI.TextPanel;

namespace Space_Engineers_LCD_MOD.Controls
{
    /// <summary>
    /// Wrapper around <see cref="IMyTerminalControl"/>, contains meta-information about the controls,
    /// the intended script to have it, and its required methods 
    /// </summary>
    public abstract class TerminalControlsWrapper
    {
        /// <summary>
        /// Gets the current selected surface from <see cref="block"/>'s terminal
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public int GetThisSurfaceIndex(IMyTerminalBlock block)
        {
            var multiTextPanel = block.Components.Get<MyMultiTextPanelComponent>();
            return multiTextPanel?.SelectedPanelIndex ?? 0;
        }

        /// <summary>
        /// Getter for controls "Visible" property
        /// </summary>
        /// <param name="block">Reference block</param>
        /// <returns>Boolean indicating if the block is visible or not</returns>
        public virtual bool Visible(IMyTerminalBlock block)
        {
            var sf = ((IMyTextSurfaceProvider)block).GetSurface(GetThisSurfaceIndex(block));
            return !string.IsNullOrEmpty(sf?.Script) && sf.ContentType == ContentType.SCRIPT &&
                   VisibleForScripts.Contains(sf.Script);
        }
        
        /// <summary>
        /// Controls to be displayed on the Terminal
        /// </summary>
        public abstract IMyTerminalControl TerminalControl { get; }
        
        
        /// <summary>
        /// List of which scripts should be selected for this control be visible
        /// </summary>
        protected virtual string[] VisibleForScripts { get; } = { "InventoryCharts", "MotorForceGraph", "RenewableGraph", "BlueprintDiagram" };
        
        /// <summary>
        /// Prefix for ID of every control
        /// </summary>
        protected virtual string IdPrefix { get; } = "Space_Engineers_LCD_MOD_";

        
        /// <summary>
        /// Create control for <see cref="TControlType"/> for <see cref="IMyTerminalBlock"/>
        /// </summary>
        /// <param name="id"></param>
        /// <typeparam name="TControlType">Type of the control</typeparam>
        /// <returns></returns>
        protected TControlType CreateControl<TControlType>(string id) =>
            MyAPIGateway.TerminalControls.CreateControl<TControlType, IMyTerminalBlock>(
                IdPrefix+id);
    }
}