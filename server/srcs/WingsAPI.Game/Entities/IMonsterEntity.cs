using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Monster;
using WingsEmu.Game.Raids;

namespace WingsEmu.Game.Entities;

public interface IMonsterEntity : INpcMonsterEntity, IMonsterAdditionalData
{
    Guid UniqueId { get; }
    ConcurrentDictionary<long, int> PlayersDamage { get; }
    DateTime LastMpRegen { get; set; }
    Position? GoToBossPosition { get; set; }
    bool IsInstantBattle { get; set; }
    IEnumerable<DropChance> RaidDrop { get; }
    DateTime LastBonusEffectTime { get; set; }
    DateTime AttentionTime { get; set; }
    ConcurrentDictionary<byte, Waypoint> Waypoints { get; set; }
    DateTime LastWayPoint { get; set; }
    byte CurrentWayPoint { get; set; }
    void GenerateDeath(IBattleEntity killer = null);
    void RefreshStats();
}