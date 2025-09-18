using System;
using System.Collections.Generic;
using Graph.Data.Scripts.Graph.Sys;
using ProtoBuf;

namespace Space_Engineers_LCD_MOD.Graph.Config
{
    [ProtoContract]
    public class ScreenProviderConfig
    {
        public bool Dirty = false;

        // ReSharper disable once UnusedMember.Global
        public ScreenProviderConfig() // Needed for Protobuf
        {
        }

        public ScreenProviderConfig(int surfaceCount, long parent)
        {
            Screens = new List<ScreenConfig>();

            for (int i = 0; i < surfaceCount; i++)
                Screens.Add(new ScreenConfig(i));

            Parent = parent;
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
                Screens.ForEach(s => s.SelectedBlocks = Array.Empty<long>()); // fail-safe deleting outdated ID's
            }
        }
    }
}