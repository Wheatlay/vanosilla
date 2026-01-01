using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Groups;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage.Configuration;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Buffs.Events;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Extensions.Mates;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Skills;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;
using WingsEmu.Plugins.BasicImplementations.Vehicles;

namespace WingsEmu.Plugins.BasicImplementations.Event.Buffs;

public class BuffRemoveEventHandler : IAsyncEventProcessor<BuffRemoveEvent>
{
    private readonly IBuffFactory _buffFactory;
    private readonly IGameLanguageService _gameLanguage;
    private readonly ISacrificeManager _sacrificeManager;
    private readonly ISpPartnerConfiguration _spPartner;
    private readonly ITeleportManager _teleportManager;
    private readonly IVehicleConfigurationProvider _vehicle;

    public BuffRemoveEventHandler(ISpPartnerConfiguration spPartner, IGameLanguageService gameLanguage, IBuffFactory buffFactory, ITeleportManager teleportManager,
        ISacrificeManager sacrificeManager, IVehicleConfigurationProvider vehicle)
    {
        _spPartner = spPartner;
        _gameLanguage = gameLanguage;
        _buffFactory = buffFactory;
        _teleportManager = teleportManager;
        _sacrificeManager = sacrificeManager;
        _vehicle = vehicle;
    }

    public async Task HandleAsync(BuffRemoveEvent e, CancellationToken cancellation)
    {
        if (e.Buffs == null)
        {
            return;
        }

        IBattleEntity battleEntity = e.Entity;

        foreach (Buff buff in e.Buffs)
        {
            if (buff == null)
            {
                continue;
            }

            if (!battleEntity.BuffComponent.HasBuff(buff.BuffId))
            {
                continue;
            }

            if (buff.IsSavingOnDisconnect() && buff.RemainingTimeInMilliseconds() > 0)
            {
                continue;
            }

            switch (buff.CardId)
            {
                case (short)BuffVnums.SPIRIT_OF_SACRIFICE:
                {
                    IBattleEntity target = _sacrificeManager.GetTarget(battleEntity);
                    if (target != null)
                    {
                        _sacrificeManager.RemoveSacrifice(battleEntity, target);
                    }

                    break;
                }
                case (short)BuffVnums.NOBLE_GESTURE:
                {
                    IBattleEntity caster = _sacrificeManager.GetCaster(battleEntity);
                    if (caster != null)
                    {
                        _sacrificeManager.RemoveSacrifice(caster, battleEntity);
                    }

                    break;
                }
            }

            battleEntity.BuffComponent.RemoveBuff(buff.BuffId);
            battleEntity.BCardComponent.RemoveBuffBCards(buff);
            battleEntity.ShadowAppears(true, buff);
            ProcessEndBuffDamage(battleEntity, buff);

            if (buff.IsConstEffect)
            {
                battleEntity.BroadcastConstBuffEffect(buff, 0);
            }

            if ((buff.IsPartnerBuff() || buff.IsBigBuff() && !buff.IsPartnerBuff() || buff.IsNoDuration() || buff.IsRefreshAtExpiration()) && !e.RemovePermanentBuff)
            {
                Buff newBuff = _buffFactory.CreateBuff(buff.CardId, buff.Caster, buff.Duration, buff.BuffFlags);
                await battleEntity.AddBuffAsync(newBuff);
                continue;
            }

            if (battleEntity is not IPlayerEntity character)
            {
                bool buffRunAway = buff.BCards.Any(x => x.Type == (short)BCardType.SpecialActions && x.SubType == (byte)AdditionalTypes.SpecialActions.RunAway);

                switch (battleEntity)
                {
                    case INpcEntity npcEntity:

                        if (buffRunAway)
                        {
                            npcEntity.IsRunningAway = false;
                        }

                        break;
                    case IMonsterEntity monsterEntity:
                        monsterEntity.RefreshStats();

                        if (buffRunAway)
                        {
                            monsterEntity.IsRunningAway = false;
                        }

                        break;
                    case IMateEntity mateEntity:
                        mateEntity.RefreshStatistics();
                        mateEntity.Owner?.Session.SendPetInfo(mateEntity, _gameLanguage);
                        mateEntity.Owner?.Session.SendCondMate(mateEntity);
                        break;
                }

                continue;
            }

            IClientSession session = character.Session;
            bool refreshHpMp = true;

            switch (buff.CardId)
            {
                case (short)BuffVnums.PRAYER_OF_DEFENCE:
                case (short)BuffVnums.ENERGY_ENHANCEMENT:
                case (short)BuffVnums.BEAR_SPIRIT:
                    if (e.ShowMessage)
                    {
                        break;
                    }

                    refreshHpMp = false;
                    break;
                case (short)BuffVnums.FAIRY_BOOSTER:
                    session.RefreshFairy();
                    break;
                case (short)BuffVnums.SPEED_BOOSTER when session.PlayerEntity.IsOnVehicle:
                    session.PlayerEntity.VehicleSpeed -= (byte)BuffVehicle(session.PlayerEntity);
                    break;
                case (short)BuffVnums.MAGIC_SPELL:
                    session.SendMsCPacket(0);
                    session.PlayerEntity.RemoveAngelElement();
                    character.CleanComboState();
                    for (int i = (int)BuffVnums.FLAME; i < (int)BuffVnums.DARKNESS; i++)
                    {
                        if (!session.PlayerEntity.BuffComponent.HasBuff(i))
                        {
                            continue;
                        }

                        session.PlayerEntity.RemoveBuffAsync(false, session.PlayerEntity.BuffComponent.GetBuff(i)).ConfigureAwait(false).GetAwaiter().GetResult();
                    }

                    break;
                case (short)BuffVnums.AMBUSH_PREPARATION_1:
                case (short)BuffVnums.AMBUSH_PREPARATION_2:
                    if (buff.RemainingTimeInMilliseconds() <= 0)
                    {
                        character.ChangeScoutState(ScoutStateType.None);
                    }

                    break;
                case (short)BuffVnums.AMBUSH_RAID:
                    character.ChangeScoutState(ScoutStateType.None);
                    break;
                case (short)BuffVnums.AMBUSH:
                    character.TriggerAmbush = false;

                    if (buff.RemainingTimeInMilliseconds() <= 0)
                    {
                        character.ChangeScoutState(ScoutStateType.None);
                    }

                    break;
            }

            if (!buff.IsBigBuff())
            {
                session.SendBfPacket(buff);
            }
            else
            {
                session.SendEmptyStaticBuffUiPacket(buff);
            }

            if (e.ShowMessage)
            {
                string message = _gameLanguage.GetLanguage(GameDialogKey.BUFF_CHATMESSAGE_EFFECT_TERMINATED, session.UserLanguage);
                string cardName = _gameLanguage.GetLanguage(GameDataType.Card, buff.Name, session.UserLanguage);

                session.SendChatMessage(string.Format(message, cardName), ChatMessageColorType.Buff);
            }

            session.RefreshStatChar(refreshHpMp);
            session.RefreshStat();
            session.SendCondPacket();

            if (buff.BCards.Any(x => x.Type == (byte)BCardType.FearSkill && x.SubType == (byte)AdditionalTypes.FearSkill.AttackRangedIncreased))
            {
                session.SendIncreaseRange();
            }

            Position position = _teleportManager.GetPosition(session.PlayerEntity.Id);
            if (position.X != 0 && position.Y != 0 && buff.CardId == (short)BuffVnums.MEMORIAL)
            {
                short savedX = _teleportManager.GetPosition(character.Id).X;
                short savedY = _teleportManager.GetPosition(character.Id).Y;
                _teleportManager.RemovePosition(session.PlayerEntity.Id);
                character.BroadcastEffectGround(EffectType.ArchmageTeleportSet, savedX, savedY, true);
            }

            await character.CheckAct52Buff(_buffFactory);

            if (buff.BCards.Any(x => x.Type == (short)BCardType.FearSkill && x.SubType == (byte)AdditionalTypes.FearSkill.MoveAgainstWill))
            {
                session.SendOppositeMove(false);
            }

            if (!buff.BCards.Any(s =>
                    s.Type == (short)BCardType.SpecialActions && s.SubType == (byte)AdditionalTypes.SpecialActions.Hide)
                && buff.CardId != (short)BuffVnums.AMBUSH && buff.CardId != (short)BuffVnums.AMBUSH_RAID)
            {
                continue;
            }

            if (buff.CardId == (short)BuffVnums.AMBUSH && character.TriggerAmbush)
            {
                continue;
            }

            if (!session.PlayerEntity.IsOnVehicle)
            {
                session.BroadcastInTeamMembers(_gameLanguage, _spPartner);
                session.RefreshParty(_spPartner);
            }

            session.UpdateVisibility();
        }
    }

    private void ProcessEndBuffDamage(IBattleEntity battleEntity, Buff buff)
    {
        if (!battleEntity.EndBuffDamages.Any())
        {
            return;
        }

        battleEntity.RemoveEndBuffDamage((short)buff.CardId);
    }

    private int BuffVehicle(IPlayerEntity c)
    {
        VehicleConfiguration vehicle = _vehicle.GetByMorph(c.Morph, c.Gender);

        if (vehicle?.VehicleBoostType == null)
        {
            return 0;
        }

        int speedToRemove = 0;

        foreach (VehicleBoost boost in vehicle.VehicleBoostType)
        {
            switch (boost.BoostType)
            {
                case BoostType.INCREASE_SPEED:
                    if (!boost.FirstValue.HasValue)
                    {
                        break;
                    }

                    speedToRemove = boost.FirstValue.Value;
                    break;
                case BoostType.CREATE_BUFF_ON_END:
                    if (!boost.FirstValue.HasValue)
                    {
                        break;
                    }

                    c.AddBuffAsync(_buffFactory.CreateBuff(boost.FirstValue.Value, c)).ConfigureAwait(false).GetAwaiter().GetResult();
                    break;
            }
        }

        return speedToRemove;
    }
}