using Graph.Data.Scripts.Graph.Sys;
using ProtoBuf;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRageMath;

namespace Space_Engineers_LCD_MOD.Networking
{
    [ProtoContract]
    class PacketSyncScreenConfig : MyEasyNetworkManager.IPacket
    {
        [ProtoMember(1)] public long BlockId { get; set; }
        [ProtoMember(2)] public ScreenProviderConfig Config { get; set; }

        // ReSharper disable once UnusedMember.Global
        public PacketSyncScreenConfig()// Needed for Protobuf
        {
        }

        public PacketSyncScreenConfig(long senderId, ScreenProviderConfig config)
        {
            BlockId = senderId;
            Config = config;
        }

        public int GetId()
        {
            return 1;
        }
    }
}