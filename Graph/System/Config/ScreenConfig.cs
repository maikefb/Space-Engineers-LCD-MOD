using System;
using System.Linq;
using Graph.Helpers;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game;
using VRageMath;

namespace Graph.System.Config
{
    [ProtoContract]
    public class ScreenConfig
    {
        const float MAX_SCALE = 2.5f;
        const float MIN_SCALE = 0.1f;
        
        // ReSharper disable once UnusedMember.Global
        public ScreenConfig() // Needed for Protobuf
        {
        }

        public ScreenConfig(int i, IMyTerminalBlock parent)
        {
            ScreenIndex = 1;
            HeaderColor = FactionHelper.GetIconColor(parent);
        }

        [ProtoMember(1)] public int ScreenIndex { get; set; }

        [ProtoMember(2)] public Color HeaderColor { get; set; }

        [ProtoMember(3)] public long[] SelectedBlocks { get; set; } = Array.Empty<long>();

        [ProtoMember(4)] public string[] SelectedGroups { get; set; } = Array.Empty<string>();

        [ProtoMember(5)] public string[] SelectedDefinition { get; set; } = Array.Empty<string>();

        [ProtoMember(6)] public string[] SelectedCategories { get; set; } = Array.Empty<string>();


        [ProtoMember(7)] public float InternalScale { get; set; } = 1;

        public MyDefinitionId[] SelectedItems
        {
            get
            {
                try
                {
                    return SelectedDefinition.Select(MyDefinitionId.Parse).ToArray();
                }
                catch (Exception e)
                {
                    ErrorHandlerHelper.LogError(e, this);
                }

                return Array.Empty<MyDefinitionId>();
            }
            set
            {
                SelectedDefinition = value.Select(a => a.ToString()).ToArray();
            }
        }

        public float Scale
        {
            get { return MathHelper.Clamp(InternalScale, MIN_SCALE, MAX_SCALE); }
            set { InternalScale = MathHelper.Clamp(value, MIN_SCALE, MAX_SCALE); }
        }

        [ProtoMember(8)] public long ReferenceBlock { get; set; }

        [ProtoMember(9)] public bool DrawLines { get; set; }
        [ProtoMember(10)] public int SortInternal { get; set; }

        public SortMethod SortMethod
        {
            get { return (SortMethod)SortInternal; }
            set { SortInternal = (int)value; }
        }

        public void CopyFrom(ScreenConfig newValue)
        {
            HeaderColor = newValue.HeaderColor;
            SelectedBlocks = newValue.SelectedBlocks;
            SelectedGroups = newValue.SelectedGroups;
            SelectedDefinition = newValue.SelectedDefinition;
            SelectedCategories = newValue.SelectedCategories;
            InternalScale = newValue.InternalScale;
            ReferenceBlock = newValue.ReferenceBlock;
            DrawLines = newValue.DrawLines;
            SortInternal = newValue.SortInternal;
        }
    }
}