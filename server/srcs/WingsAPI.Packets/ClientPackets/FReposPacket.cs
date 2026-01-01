// WingsEmu
// 
// Developed by NosWings Team

namespace WingsEmu.Packets.ClientPackets
{
    [PacketHeader("f_repos")]
    public class FReposPacket : ClientPacket
    {
        #region Properties

        [PacketIndex(0)]
        public short OldSlot { get; set; }

        [PacketIndex(1)]
        public short Amount { get; set; }

        [PacketIndex(2)]
        public short NewSlot { get; set; }

        [PacketIndex(3)]
        public byte? Unknown { get; set; }

        #endregion
    }
}