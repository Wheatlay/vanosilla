using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Quicklist;
using WingsEmu.DTOs.Skills;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage.Configuration;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Networking.Broadcasting;
using WingsEmu.Game.Raids;
using WingsEmu.Game.Skills;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class SpTransformEventHandler : IAsyncEventProcessor<SpTransformEvent>
{
    private readonly IBuffFactory _buffFactory;
    private readonly ICharacterAlgorithm _characterAlgorithm;
    private readonly IGameLanguageService _languageService;
    private readonly IRankingManager _rankingManager;
    private readonly IReputationConfiguration _reputationConfiguration;
    private readonly ISkillsManager _skillsManager;
    private readonly ISpWingConfiguration _spWingConfiguration;

    public SpTransformEventHandler(IGameLanguageService languageService, ISkillsManager skillsManager, ISpWingConfiguration spWingConfiguration, IBuffFactory buffFactory,
        ICharacterAlgorithm characterAlgorithm, IReputationConfiguration reputationConfiguration, IRankingManager rankingManager)
    {
        _languageService = languageService;
        _skillsManager = skillsManager;
        _spWingConfiguration = spWingConfiguration;
        _buffFactory = buffFactory;
        _characterAlgorithm = characterAlgorithm;
        _reputationConfiguration = reputationConfiguration;
        _rankingManager = rankingManager;
    }

    public async Task HandleAsync(SpTransformEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        GameItemInstance specialist = e.Specialist;
        GameItemInstance fairy = session.PlayerEntity.Fairy;

        if (session.PlayerEntity.IsCastingSkill)
        {
            return;
        }

        if (specialist == null)
        {
            session.SendMsg(_languageService.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_NO_SPECIALIST_CARD, session.UserLanguage), MsgMessageType.Middle);
            session.PlayerEntity.BroadcastEndDancingGuriPacket();
            return;
        }

        if (specialist.GameItem.IsPartnerSpecialist)
        {
            session.PlayerEntity.BroadcastEndDancingGuriPacket();
            return;
        }

        if (session.PlayerEntity.CharacterSkills.Any(s => !session.PlayerEntity.SkillCanBeUsed(s.Value)))
        {
            session.SendMsg(_languageService.GetLanguage(GameDialogKey.SKILL_SHOUTMESSAGE_SKILLS_IN_LOADING, session.UserLanguage), MsgMessageType.Middle);
            session.PlayerEntity.BroadcastEndDancingGuriPacket();
            return;
        }

        if ((byte)session.PlayerEntity.GetReputationIcon(_reputationConfiguration, _rankingManager.TopReputation) < specialist.GameItem.ReputationMinimum)
        {
            session.SendMsg(_languageService.GetLanguage(GameDialogKey.INFORMATION_CHATMESSAGE_NOT_ENOUGH_REPUT, session.UserLanguage), MsgMessageType.Middle);
            session.PlayerEntity.BroadcastEndDancingGuriPacket();
            return;
        }

        if (fairy != null && specialist.GameItem.Element != 0 && fairy.GameItem.Element != specialist.GameItem.Element && fairy.GameItem.Element != (byte)ElementType.All)
        {
            session.SendMsg(_languageService.GetLanguage(GameDialogKey.ITEM_SHOUTMESSAGE_FAIRY_WRONG_ELEMENT, session.UserLanguage), MsgMessageType.Middle);
            session.PlayerEntity.BroadcastEndDancingGuriPacket();
            return;
        }

        if (!session.PlayerEntity.IsSpCooldownElapsed())
        {
            session.SendMsg(_languageService.GetLanguageFormat(GameDialogKey.SPECIALIST_SHOUTMESSAGE_IN_COOLDOWN, session.UserLanguage, session.PlayerEntity.GetSpCooldown()), MsgMessageType.Middle);
            session.PlayerEntity.BroadcastEndDancingGuriPacket();
            return;
        }

        await session.PlayerEntity.RemoveBuffsOnSpTransformAsync();

        session.PlayerEntity.BCardComponent.AddEquipmentBCards(EquipmentType.Sp, specialist.GameItem.BCards);
        session.RefreshSpPoint();
        session.PlayerEntity.LastTransform = DateTime.UtcNow;
        session.PlayerEntity.LastSkillCombo = null;
        session.PlayerEntity.UseSp = true;
        session.PlayerEntity.Morph = specialist.GameItem.Morph;
        session.PlayerEntity.MorphUpgrade = specialist.Upgrade;
        session.PlayerEntity.MorphUpgrade2 = specialist.Design;

        session.BroadcastCMode();
        session.PlayerEntity.BroadcastEndDancingGuriPacket();
        session.RefreshLevel(_characterAlgorithm);
        session.BroadcastEffect(EffectType.Transform, new RangeBroadcast(session.PlayerEntity.PositionX, session.PlayerEntity.PositionY));
        session.PlayerEntity.SpecialistComponent.RefreshSlStats();
        session.RefreshSpPoint();
        session.RefreshStatChar();
        session.RefreshStat(true);
        session.SendCondPacket();
        session.SendIncreaseRange();
        session.PlayerEntity.ChargeComponent.ResetCharge();
        session.PlayerEntity.BCardComponent.ClearChargeBCard();

        if (session.PlayerEntity.IsInRaidParty)
        {
            foreach (IClientSession s in session.PlayerEntity.Raid.Members)
            {
                s.RefreshRaidMemberList();
            }
        }

        session.PlayerEntity.SkillsSp = new ConcurrentDictionary<int, CharacterSkill>();
        session.PlayerEntity.Skills.Clear();
        foreach (SkillDTO skill in _skillsManager.GetSkills())
        {
            if (!session.PlayerEntity.Specialist.IsSpSkill(skill))
            {
                continue;
            }

            var newSkill = new CharacterSkill
            {
                SkillVNum = skill.Id
            };

            session.PlayerEntity.SkillsSp[skill.Id] = newSkill;
            session.PlayerEntity.Skills.Add(newSkill);
        }

        session.RefreshSkillList();
        session.PlayerEntity.ClearSkillCooldowns();
        session.RefreshQuicklist();

        SpWingInfo wingInfo = _spWingConfiguration.GetSpWingInfo(specialist.Design);
        if (wingInfo == null)
        {
            return;
        }

        IEnumerable<Buff> buffs = wingInfo.Buffs.Select(buff => _buffFactory.CreateBuff(buff.BuffId, session.PlayerEntity, buff.IsPermanent ? BuffFlag.NO_DURATION : BuffFlag.NORMAL));
        await session.PlayerEntity.AddBuffsAsync(buffs);
    }
}