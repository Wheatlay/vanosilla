using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WingsEmu.Core.Extensions;
using WingsEmu.Game.Entities;

namespace WingsEmu.Game.Managers;

public interface IDelayConfiguration
{
    TimeSpan GetDelayByAction(DelayedActionType type);
}

public class DelayConfiguration : IDelayConfiguration
{
    private static readonly TimeSpan Default = TimeSpan.FromSeconds(3);

    private readonly Dictionary<DelayedActionType, TimeSpan> _times = new()
    {
        [DelayedActionType.SummonPet] = Default,
        [DelayedActionType.KickPet] = Default,
        [DelayedActionType.EquipVehicle] = Default,
        [DelayedActionType.IceBreakerUnfreeze] = Default,
        [DelayedActionType.PartnerWearSp] = TimeSpan.FromSeconds(5),
        [DelayedActionType.PartnerLearnSkill] = TimeSpan.FromSeconds(5),
        [DelayedActionType.WearSp] = TimeSpan.FromSeconds(5),
        [DelayedActionType.ReturnWing] = TimeSpan.FromSeconds(5),
        [DelayedActionType.ReturnAmulet] = TimeSpan.FromSeconds(5),
        [DelayedActionType.MinilandBell] = TimeSpan.FromSeconds(5),
        [DelayedActionType.BaseTeleporter] = TimeSpan.FromSeconds(5),
        [DelayedActionType.LodScroll] = TimeSpan.FromSeconds(5),
        [DelayedActionType.PartnerResetSkill] = TimeSpan.FromSeconds(5),
        [DelayedActionType.PartnerResetAllSkills] = TimeSpan.FromSeconds(5),
        [DelayedActionType.WingOfFriendship] = Default,
        [DelayedActionType.ButtonSwitch] = TimeSpan.FromSeconds(2),
        [DelayedActionType.Mining] = default,
        [DelayedActionType.SealedVessel] = TimeSpan.FromSeconds(2),
        [DelayedActionType.RainbowBattleCaptureFlag] = TimeSpan.FromSeconds(5),
        [DelayedActionType.RainbowBattleUnfreeze] = TimeSpan.FromSeconds(5)
    };

    public TimeSpan GetDelayByAction(DelayedActionType type) => _times.GetOrDefault(type, Default);
}

public interface IDelayManager
{
    ValueTask<DateTime> RegisterAction(IBattleEntity entity, DelayedActionType action, TimeSpan time = default);
    ValueTask<bool> CanPerformAction(IBattleEntity entity, DelayedActionType type);
    ValueTask<bool> CompleteAction(IBattleEntity entity, DelayedActionType action);
}

public enum DelayedActionType
{
    KickPet,
    SummonPet,
    EquipVehicle,
    WearSp,
    ReturnWing,
    ReturnAmulet,
    MinilandBell,
    LodScroll,
    ReturnScroll,
    MorphScroll,
    UseTeleporter,
    BaseTeleporter,
    PartnerWearSp,
    PartnerLearnSkill,
    PartnerResetSkill,
    PartnerResetAllSkills,
    IceBreakerUnfreeze,
    WingOfFriendship,
    ButtonSwitch,
    Mining,
    SealedVessel,
    RainbowBattleCaptureFlag,
    RainbowBattleUnfreeze
}