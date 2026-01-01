using WingsEmu.Packets.Enums;

namespace WingsEmu.Game.Entities;

public interface IMonsterAdditionalData
{
    bool IsMateTrainer { get; }
    bool IsBonus { get; set; }
    bool IsBoss { get; }
    bool IsTarget { get; }
    bool IsMoving { get; }
    bool VesselMonster { get; }
    long? SummonerId { get; }
    VisualType? SummonerType { get; }
    SummonType? SummonType { get; }
    bool IsHostile { get; }
}