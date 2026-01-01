// WingsEmu
// 
// Developed by NosWings Team

using WingsEmu.Packets.Enums.Character;

namespace WingsEmu.Packets.ClientPackets
{
    [PacketHeader("gop")]
    public class CharacterOptionPacket : ClientPacket
    {
        [PacketIndex(0)]
        public CharacterOption Option { get; set; }

        [PacketIndex(1)]
        public bool IsActive { get; set; }
    }
}