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

        public ScreenProviderConfig(int surfaceCount)
        {
            Screens = new List<ScreenConfig>();

            for (int i = 0; i < surfaceCount; i++)
                Screens.Add(new ScreenConfig(i));
        }

        [ProtoMember(1)] public List<ScreenConfig> Screens { get; set; }
    }
}