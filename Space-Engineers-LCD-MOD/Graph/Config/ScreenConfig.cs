using System;
using ProtoBuf;
using VRageMath;

namespace Graph.Data.Scripts.Graph.Sys
{
    [ProtoContract]
    public class ScreenConfig
    {
        // ReSharper disable once UnusedMember.Global
        public ScreenConfig() // Needed for Protobuf
        {
        } 

        public ScreenConfig(int i)
        {
            ScreenIndex = 1;
        }

        [ProtoMember(1)] public int ScreenIndex { get; set; }

        [ProtoMember(2)] public Color HeaderColor { get; set; } = new Color(54, 0, 63);

        [ProtoMember(3)] public long[] SelectedBlocks { get; set; } = Array.Empty<long>();

        [ProtoMember(4)] public string[] SelectedItems { get; set; } = Array.Empty<string>();

        public void CopyFrom(ScreenConfig newValue)
        {
            HeaderColor = newValue.HeaderColor;
            SelectedBlocks = newValue.SelectedBlocks;
            SelectedItems = newValue.SelectedItems;
        }
    }
}