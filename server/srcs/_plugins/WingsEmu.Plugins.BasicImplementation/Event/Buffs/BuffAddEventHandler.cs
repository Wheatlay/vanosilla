using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Core.Extensions;
using WingsEmu.DTOs.BCards;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Buffs.Events;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Extensions.Mates;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Skills;
using WingsEmu.Packets.Enums.Chat;
using WingsEmu.Packets.ServerPackets;

namespace WingsEmu.Plugins.BasicImplementations.Event.Buffs;

public class BuffAddEventHandler : IAsyncEventProcessor<BuffAddEvent>
{
    private readonly IBCardEffectHandlerContainer _bCardEffectHandlerContainer;
    private readonly IBuffFactory _buffFactory;
    private readonly IGameLanguageService _gameLanguage;

    public BuffAddEventHandler(IBuffFactory buffFactory, IGameLanguageService gameLanguage, IBCardEffectHandlerContainer bCardEffectHandlerContainer)
    {
        _buffFactory = buffFactory;
        _gameLanguage = gameLanguage;
        _bCardEffectHandlerContainer = bCardEffectHandlerContainer;
    }

    public async Task HandleAsync(BuffAddEvent e, CancellationToken cancellation)
    {
        IBattleEntity battleEntity = e.Entity;
        foreach (Buff buff in e.Buffs)
        {
            if (buff == null)
            {
                continue;
            }

            if (buff.ElementType != ElementType.Neutral && (ElementType)battleEntity.Element != buff.ElementType)
            {
                continue;
            }

            switch (battleEntity)
            {
                case IMonsterEntity { CanBeDebuffed: false } when !buff.IsBigBuff():
                    continue;
            }

            Buff soundFlowerBuff = battleEntity.BuffComponent.GetBuff((short)BuffVnums.SOUND_FLOWER_BLESSING_BETTER);
            if (soundFlowerBuff != null && buff.CardId == (short)BuffVnums.SOUND_FLOWER_BLESSING)
            {
                // Refresh sound flower buff duration
                soundFlowerBuff.SetBuffDuration(soundFlowerBuff.Duration);

                if (battleEntity is IPlayerEntity playerEntity)
                {
                    playerEntity.Session.SendBfPacket(soundFlowerBuff, 0);
                }

                continue;
            }

            bool showMessage = true;
            Buff existingBuffWithSameId = battleEntity.BuffComponent.GetBuff(buff.CardId);
            if (existingBuffWithSameId != null && buff.IsNormal() && !buff.IsPartnerBuff())
            {
                await battleEntity.EmitEventAsync(new BuffRemoveEvent
                {
                    Entity = battleEntity,
                    Buffs = Lists.Create(existingBuffWithSameId),
                    RemovePermanentBuff = false,
                    ShowMessage = false
                });

                showMessage = false;
            }

            Buff existingBuffByGroupId = battleEntity.BuffComponent.GetBuffByGroupId(buff.GroupId);
            if (existingBuffByGroupId != null && !existingBuffByGroupId.IsBigBuff() && !buff.IsBigBuff())
            {
                showMessage = false;
                if (existingBuffByGroupId.Level > buff.Level)
                {
                    continue;
                }

                if (existingBuffByGroupId.Level == buff.Level)
                {
                    existingBuffByGroupId.SetBuffDuration(buff.Duration);

                    if (battleEntity is IPlayerEntity playerEntity)
                    {
                        playerEntity.Session.SendBfPacket(existingBuffByGroupId, 0);
                    }

                    continue;
                }

                await battleEntity.EmitEventAsync(new BuffRemoveEvent
                {
                    Entity = battleEntity,
                    Buffs = Lists.Create(existingBuffByGroupId),
                    RemovePermanentBuff = false,
                    ShowMessage = false,
                    RemoveFromGroupId = false
                });
            }

            foreach (BCardDTO bCard in buff.BCards.Where(x => (!x.IsSecondBCardExecution.HasValue || !x.IsSecondBCardExecution.Value) && !x.TickPeriod.HasValue))
            {
                _bCardEffectHandlerContainer.Execute(battleEntity, buff.Caster, bCard);
            }

            switch (battleEntity)
            {
                case IPlayerEntity character:
                {
                    IClientSession session = character.Session;

                    switch (buff.CardId)
                    {
                        case (short)BuffVnums.MAGICAL_FETTERS:
                            if (character.HasBuff(BuffVnums.MAGIC_SPELL))
                            {
                                await character.RemoveBuffAsync(false, character.BuffComponent.GetBuff((short)BuffVnums.MAGIC_SPELL));
                            }

                            break;
                        case (short)BuffVnums.AMBUSH_PREPARATION_1:
                            character.ChangeScoutState(ScoutStateType.FirstState);
                            break;
                        case (short)BuffVnums.AMBUSH_PREPARATION_2:
                            character.ChangeScoutState(ScoutStateType.SecondState);
                            break;
                        case (short)BuffVnums.AMBUSH:
                            switch (character.ScoutStateType)
                            {
                                case ScoutStateType.FirstState:
                                    Buff toAdd = _buffFactory.CreateBuff((short)BuffVnums.AMBUSH_POSITION_1, character, buff.Duration);
                                    await character.AddBuffAsync(toAdd);

                                    Buff buffToRemove = character.BuffComponent.GetBuff((short)BuffVnums.AMBUSH_PREPARATION_1);
                                    await character.RemoveBuffAsync(false, buffToRemove);
                                    break;
                                case ScoutStateType.SecondState:
                                    Buff toAddSecond = _buffFactory.CreateBuff((short)BuffVnums.AMBUSH_POSITION_2, character, buff.Duration);
                                    await character.AddBuffAsync(toAddSecond);

                                    Buff secondBuffToRemove = character.BuffComponent.GetBuff((short)BuffVnums.AMBUSH_PREPARATION_2);
                                    await character.RemoveBuffAsync(false, secondBuffToRemove);
                                    break;
                            }

                            break;
                        case (short)BuffVnums.AMBUSH_RAID:
                            switch (character.ScoutStateType)
                            {
                                case ScoutStateType.FirstState:
                                    Buff toAdd = _buffFactory.CreateBuff((short)BuffVnums.SNIPER_POSITION_1, character, buff.Duration);
                                    await character.AddBuffAsync(toAdd);

                                    Buff buffToRemove = character.BuffComponent.GetBuff((short)BuffVnums.AMBUSH_POSITION_1);
                                    await character.RemoveBuffAsync(false, buffToRemove);
                                    break;
                                case ScoutStateType.SecondState:
                                    Buff toAddSecond = _buffFactory.CreateBuff((short)BuffVnums.SNIPER_POSITION_2, character, buff.Duration);
                                    await character.AddBuffAsync(toAddSecond);

                                    Buff secondBuffToRemove = character.BuffComponent.GetBuff((short)BuffVnums.AMBUSH_POSITION_2);
                                    await character.RemoveBuffAsync(false, secondBuffToRemove);
                                    break;
                            }

                            break;
                    }

                    if (!buff.IsBigBuff())
                    {
                        switch (buff.CardId)
                        {
                            case (short)BuffVnums.CHARGE when session.PlayerEntity.BCardComponent.GetChargeBCards().Any():
                                int sum = session.PlayerEntity.BCardComponent.GetChargeBCards().Sum(x => x.FirstDataValue(session.PlayerEntity.Level));
                                session.SendBfPacket(buff, sum, sum);
                                break;
                            case (short)BuffVnums.CHARGE:
                                session.SendBfPacket(buff, session.PlayerEntity.ChargeComponent.GetCharge(), session.PlayerEntity.ChargeComponent.GetCharge());
                                break;
                            default:
                                session.SendBfPacket(buff, 0);
                                break;
                        }
                    }
                    else
                    {
                        session.SendStaticBuffUiPacket(buff, buff.RemainingTimeInMilliseconds());
                    }

                    if (showMessage)
                    {
                        string message = _gameLanguage.GetLanguage(GameDialogKey.BUFF_CHATMESSAGE_UNDER_EFFECT, session.UserLanguage);
                        string cardName = _gameLanguage.GetLanguage(GameDataType.Card, buff.Name, session.UserLanguage);

                        session.SendChatMessage(string.Format(message, cardName), !buff.IsSavingOnDisconnect() ? ChatMessageColorType.Buff : ChatMessageColorType.Red);
                    }

                    break;
                }
            }

            if (buff.IsConstEffect)
            {
                battleEntity.BroadcastConstBuffEffect(buff, 0);
                battleEntity.BroadcastConstBuffEffect(buff, (int)buff.Duration.TotalMilliseconds);
            }

            if (buff.EffectId > 0 && !buff.IsConstEffect)
            {
                var effect = new EffectServerPacket
                {
                    EffectType = (byte)battleEntity.Type,
                    CharacterId = battleEntity.Id,
                    Id = buff.EffectId
                };

                battleEntity.MapInstance?.Broadcast(effect);
            }

            battleEntity.BuffComponent.AddBuff(buff);
            battleEntity.BCardComponent.AddBuffBCards(buff);

            battleEntity.ShadowAppears(false, buff);
            switch (battleEntity)
            {
                case IPlayerEntity c:
                    await c.CheckAct52Buff(_buffFactory);
                    c.Session.RefreshStatChar();
                    c.Session.RefreshStat();
                    c.Session.SendCondPacket();
                    c.Session.SendIncreaseRange();
                    c.Session.UpdateVisibility();
                    break;
                case IMonsterEntity monsterEntity:
                    monsterEntity.RefreshStats();
                    break;
                case IMateEntity mateEntity:
                    mateEntity.Owner?.Session.SendPetInfo(mateEntity, _gameLanguage);
                    mateEntity.Owner?.Session.SendCondMate(mateEntity);
                    break;
            }
        }
    }
}