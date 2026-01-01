// WingsEmu
// 
// Developed by NosWings Team

namespace WingsEmu.Packets.ClientPackets
{
    [PacketHeader("repos")]
    public class ReposPacket : ClientPacket
    {
        #region Properties

        [PacketIndex(0)]
        public short OldSlot { get; set; }

        [PacketIndex(1)]
        public short Amount { get; set; }

        [PacketIndex(2)]
        public short NewSlot { get; set; }

        [PacketIndex(3)]
        public bool IsPartnerBackpack { get; set; }

        #endregion
    }
}