using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Quicklist;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage.Configuration;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Networking.Broadcasting;
using WingsEmu.Game.Raids;
using WingsEmu.Game.Skills;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class SpUntransformEventHandler : IAsyncEventProcessor<SpUntransformEvent>
{
    private readonly ICharacterAlgorithm _characterAlgorithm;
    private readonly IGameLanguageService _languageService;
    private readonly IMeditationManager _meditationManager;
    private readonly ISpWingConfiguration _spWingConfiguration;
    private readonly ISpyOutManager _spyOutManager;
    private readonly ITeleportManager _teleportManager;

    public SpUntransformEventHandler(IGameLanguageService languageService, ISpWingConfiguration spWingConfiguration,
        ICharacterAlgorithm characterAlgorithm, ISpyOutManager spyOutManager, IMeditationManager meditationManager, ITeleportManager teleportManager)
    {
        _languageService = languageService;
        _spWingConfiguration = spWingConfiguration;
        _characterAlgorithm = characterAlgorithm;
        _meditationManager = meditationManager;
        _teleportManager = teleportManager;
        _spyOutManager = spyOutManager;
    }

    public async Task HandleAsync(SpUntransformEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;

        SpWingInfo wingInfo = _spWingConfiguration.GetSpWingInfo(session.PlayerEntity.MorphUpgrade2);
        if (wingInfo != null)
        {
            foreach (WingBuff buff in wingInfo.Buffs)
            {
                Buff wingBuff = session.PlayerEntity.BuffComponent.GetBuff(buff.BuffId);
                await session.PlayerEntity.RemoveBuffAsync(buff.IsPermanent, wingBuff);
            }
        }

        await session.PlayerEntity.RemoveBuffsOnSpTransformAsync();

        session.PlayerEntity.BCardComponent.ClearEquipmentBCards(EquipmentType.Sp);

        session.PlayerEntity.RemoveAngelElement();
        session.PlayerEntity.ChangeScoutState(ScoutStateType.None);
        session.PlayerEntity.CleanComboState();
        session.SendMsCPacket(1);
        session.PlayerEntity.UseSp = false;
        session.PlayerEntity.LastSkillCombo = null;
        session.RefreshLevel(_characterAlgorithm);

        int cooldown = 30;
        if (session.PlayerEntity.SkillsSp != null)
        {
            foreach ((int skillVnum, CharacterSkill skill) in session.PlayerEntity.SkillsSp)
            {
                if (session.PlayerEntity.SkillCanBeUsed(skill))
                {
                    continue;
                }

                short time = skill.Skill.Cooldown;
                double temp = (skill.LastUse - DateTime.UtcNow).TotalMilliseconds + time * 100;
                temp /= 2000;
                cooldown = temp > cooldown ? (int)temp : cooldown;
            }
        }

        if (_spyOutManager.ContainsSpyOut(session.PlayerEntity.Id))
        {
            session.SendObArPacket();
            _spyOutManager.RemoveSpyOutSkill(session.PlayerEntity.Id);
        }

        session.SendIncreaseRange();
        session.PlayerEntity.ChargeComponent.ResetCharge();
        session.PlayerEntity.BCardComponent.ClearChargeBCard();
        await session.EmitEventAsync(new GetDefaultMorphEvent());
        session.PlayerEntity.SpCooldownEnd = DateTime.UtcNow.AddSeconds(cooldown);
        session.SendChatMessage(_languageService.GetLanguageFormat(GameDialogKey.INFORMATION_CHATMESSAGE_SP_STAY_TIME, session.UserLanguage, cooldown), ChatMessageColorType.Red);
        session.SendSpCooldownUi(cooldown);
        session.BroadcastCMode();
        session.BroadcastGuri(6, 0, rules: new RangeBroadcast(session.PlayerEntity.PositionX, session.PlayerEntity.PositionY));
        _meditationManager.RemoveAllMeditation(session.PlayerEntity);
        _teleportManager.RemovePosition(session.PlayerEntity.Id);
        session.PlayerEntity.SkillComponent.SendTeleportPacket = null;

        session.PlayerEntity.IsRemovingSpecialistPoints = false;
        session.PlayerEntity.InitialScpPacketSent = false;
        session.PlayerEntity.Session.SendScpPacket(0);

        if (session.PlayerEntity.IsInRaidParty)
        {
            foreach (IClientSession s in session.PlayerEntity.Raid.Members)
            {
                s.RefreshRaidMemberList();
            }
        }

        session.RefreshSkillList();
        session.PlayerEntity.ClearSkillCooldowns();
        session.PlayerEntity.Skills.Clear();
        foreach (IBattleEntitySkill skill in session.PlayerEntity.GetSkills())
        {
            session.PlayerEntity.Skills.Add(skill);
        }

        session.RefreshQuicklist();
        session.RefreshStatChar();
        session.RefreshEquipment();
        session.RefreshStat(true);
        session.SendCondPacket();
    }
}