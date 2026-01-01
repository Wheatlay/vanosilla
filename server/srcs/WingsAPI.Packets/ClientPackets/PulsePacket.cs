// WingsEmu
// 
// Developed by NosWings Team

namespace WingsEmu.Packets.ClientPackets
{
    [PacketHeader("pulse")]
    public class PulsePacket : ClientPacket
    {
        #region Properties

        [PacketIndex(0)]
        public int Tick { get; set; }

        #endregion
    }
}