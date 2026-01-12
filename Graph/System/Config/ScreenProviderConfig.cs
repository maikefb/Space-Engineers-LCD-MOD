using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.ModAPI;

namespace Graph.System.Config
{
    [ProtoContract]
    public class ScreenProviderConfig
    {
        public ScreenProviderConfig() 
        {
        }

        public ScreenProviderConfig(int surfaceCount, IMyTerminalBlock parent)
        {
            Screens = new List<ScreenConfig>();

            for (int i = 0; i < surfaceCount; i++)
                Screens.Add(new ScreenConfig(i, parent));

            Parent = parent.CubeGrid.EntityId;
        }

        [ProtoMember(1)] public List<ScreenConfig> Screens { get; set; }


        [ProtoMember(2)] long Parent { get; set; }

        public long ParentGrid
        {
            get
            {
                return Parent;
            }
            set
            {
                Parent = value;
                
                // todo: Some Extra logic is Required to properly migrate blocks ids when creating Blueprints
                Screens?.ForEach(s => s.SelectedBlocks = Array.Empty<long>()); // fail-safe deleting outdated ID's
            }
        }

        public void CopyFrom(ScreenProviderConfig other)
        {
            if (Screens.Count != other.Screens.Count)
                return;
                        
            for (var index = 0; index < Screens.Count; index++)
                Screens[index].CopyFrom(other.Screens[index]);
        }
    }
}