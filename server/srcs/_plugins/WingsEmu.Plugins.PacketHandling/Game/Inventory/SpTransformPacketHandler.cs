using System;
using System.Threading.Tasks;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Networking.Broadcasting;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.PacketHandling.Game.Inventory;

public class SpTransformPacketHandler : GenericGamePacketHandlerBase<SpTransformPacket>
{
    private readonly ICharacterAlgorithm _algorithm;
    private readonly IDelayManager _delayManager;
    private readonly IGameLanguageService _language;

    public SpTransformPacketHandler(IGameLanguageService language, IDelayManager delayManager, ICharacterAlgorithm algorithm)
    {
        _language = language;
        _delayManager = delayManager;
        _algorithm = algorithm;
    }

    protected override async Task HandlePacketAsync(IClientSession session, SpTransformPacket spTransformPacket)
    {
        GameItemInstance specialistInstance = session.PlayerEntity.Specialist;

        if (specialistInstance == null)
        {
            session.SendMsg(_language.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_NO_SPECIALIST_CARD, session.UserLanguage), MsgMessageType.Middle);
            return;
        }

        if (!session.PlayerEntity.IsAlive())
        {
            return;
        }

        if (session.PlayerEntity.IsSeal)
        {
            return;
        }

        if (spTransformPacket.Type == 10)
        {
            short specialistDamage = spTransformPacket.SpecialistDamage;
            short specialistDefense = spTransformPacket.SpecialistDefense;
            short specialistElement = spTransformPacket.SpecialistElement;
            short specialistHealth = spTransformPacket.SpecialistHp;
            int transportId = spTransformPacket.TransportId;
            if (!session.PlayerEntity.UseSp || transportId != specialistInstance.TransportId)
            {
                session.SendMsg(_language.GetLanguage(GameDialogKey.SPECIALIST_SHOUTMESSAGE_USE_NEEDED, session.UserLanguage), MsgMessageType.Middle);
                return;
            }

            if (specialistDamage < 0 || specialistDefense < 0 || specialistElement < 0 || specialistHealth < 0)
            {
                return;
            }

            if (specialistInstance.SlDamage + specialistDamage + specialistInstance.SlElement + specialistElement +
                specialistInstance.SlHP + specialistHealth + specialistInstance.SlDefence + specialistDefense > specialistInstance.SpPointsBasic())
            {
                return;
            }

            specialistInstance.SlDamage += specialistDamage;
            specialistInstance.SlDefence += specialistDefense;
            specialistInstance.SlElement += specialistElement;
            specialistInstance.SlHP += specialistHealth;

            session.PlayerEntity.SpecialistComponent.RefreshSlStats();

            session.RefreshStatChar();
            session.RefreshStat();
            session.SendSpecialistCardInfo(specialistInstance, _algorithm);
            session.SendMsg(_language.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_SP_POINTS_SET, session.UserLanguage), MsgMessageType.Middle);

            return;
        }

        if (session.PlayerEntity.IsSitting)
        {
            await session.RestAsync();
        }

        if (session.PlayerEntity.BuffComponent.HasBuff(BuffGroup.Bad))
        {
            session.SendMsg(_language.GetLanguage(GameDialogKey.SPECIALIST_SHOUTMESSAGE_NO_REMOVE_DEBUFFS, session.UserLanguage), MsgMessageType.Middle);
            session.PlayerEntity.BroadcastEndDancingGuriPacket();
            return;
        }

        if (session.PlayerEntity.IsOnVehicle)
        {
            session.SendMsg(_language.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_REMOVE_VEHICLE, session.UserLanguage), MsgMessageType.Middle);
            return;
        }

        if (session.PlayerEntity.IsMorphed)
        {
            session.SendChatMessage(_language.GetLanguage(GameDialogKey.ITEM_MESSAGE_CANT_USE, session.UserLanguage), ChatMessageColorType.Yellow);
            session.PlayerEntity.BroadcastEndDancingGuriPacket();
            return;
        }

        if (session.PlayerEntity.UseSp)
        {
            if (session.PlayerEntity.IsCastingSkill)
            {
                return;
            }

            if (session.PlayerEntity.LastSkillUse.AddSeconds(3) > DateTime.UtcNow)
            {
                return;
            }

            await session.EmitEventAsync(new SpUntransformEvent());
            return;
        }

        if (!session.PlayerEntity.IsSpCooldownElapsed())
        {
            session.SendMsg(_language.GetLanguageFormat(GameDialogKey.SPECIALIST_SHOUTMESSAGE_IN_COOLDOWN, session.UserLanguage, session.PlayerEntity.GetSpCooldown()), MsgMessageType.Middle);
            return;
        }

        if (session.PlayerEntity.LastSkillUse.AddSeconds(2) >= DateTime.UtcNow)
        {
            return;
        }

        if (spTransformPacket.Type == 1)
        {
            bool canWearSp = await _delayManager.CanPerformAction(session.PlayerEntity, DelayedActionType.WearSp);
            if (!canWearSp)
            {
                return;
            }

            await _delayManager.CompleteAction(session.PlayerEntity, DelayedActionType.WearSp);
            await session.EmitEventAsync(new SpTransformEvent
            {
                Specialist = specialistInstance
            });
        }
        else
        {
            DateTime waitUntil = await _delayManager.RegisterAction(session.PlayerEntity, DelayedActionType.WearSp);
            session.SendDelay((int)(waitUntil - DateTime.UtcNow).TotalMilliseconds, GuriType.Transforming, "sl 1");
            session.BroadcastGuri(2, 1, 0, new RangeBroadcast(session.PlayerEntity.PositionX, session.PlayerEntity.PositionY));
        }
    }
}