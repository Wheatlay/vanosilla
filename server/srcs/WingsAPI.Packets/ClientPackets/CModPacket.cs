// WingsEmu
// 
// Developed by NosWings Team

namespace WingsEmu.Packets.ClientPackets
{
    [PacketHeader("c_mod")]
    public class CmodPacket : ClientPacket
    {
        #region Properties

        [PacketIndex(0)]
        public long BazaarId { get; set; }

        [PacketIndex(1)]
        public short VNum { get; set; }

        [PacketIndex(2)]
        public int Amount { get; set; }

        [PacketIndex(3)]
        public long NewPricePerItem { get; set; }

        [PacketIndex(4)]
        public byte Confirmed { get; set; }

        #endregion
    }
}