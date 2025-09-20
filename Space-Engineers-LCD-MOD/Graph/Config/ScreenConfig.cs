using System;
using System.Linq;
using ProtoBuf;
using Space_Engineers_LCD_MOD.Helpers;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Graph.Data.Scripts.Graph.Sys
{
    [ProtoContract]
    public class ScreenConfig
    {
        MyDefinitionId[] _selectedItems;

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
            set { SelectedDefinition = value.Select(a => a.ToString()).ToArray(); }
        }

        public float Scale
        {
            get
            {
                return MathHelper.Clamp(InternalScale, 0.1f, 10f);
            }
            set
            {
                InternalScale = MathHelper.Clamp(value, 0.1f, 10f);
            }
        } 

        public void CopyFrom(ScreenConfig newValue)
        {
            HeaderColor = newValue.HeaderColor;
            SelectedBlocks = newValue.SelectedBlocks;
            SelectedGroups = newValue.SelectedGroups;
            SelectedItems = newValue.SelectedItems;
            SelectedCategories = newValue.SelectedCategories;
        }
    }
}