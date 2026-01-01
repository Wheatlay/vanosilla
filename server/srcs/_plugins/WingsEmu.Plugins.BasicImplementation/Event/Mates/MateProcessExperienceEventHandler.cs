using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Groups;
using WingsAPI.Packets.Enums;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage.Configuration;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Extensions.Mates;
using WingsEmu.Game.Helpers;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Mates.Events;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Mates;

public class MateProcessExperienceEventHandler : IAsyncEventProcessor<MateProcessExperienceEvent>
{
    private readonly ICharacterAlgorithm _characterAlgorithm;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IServerManager _serverManager;
    private readonly ISpPartnerConfiguration _spPartnerConfiguration;

    public MateProcessExperienceEventHandler(IServerManager serverManager, IGameLanguageService gameLanguage, ICharacterAlgorithm characterAlgorithm, ISpPartnerConfiguration spPartnerConfiguration)
    {
        _serverManager = serverManager;
        _gameLanguage = gameLanguage;
        _characterAlgorithm = characterAlgorithm;
        _spPartnerConfiguration = spPartnerConfiguration;
    }

    public async Task HandleAsync(MateProcessExperienceEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        IPlayerEntity character = session.PlayerEntity;

        if (character.IsOnVehicle)
        {
            return;
        }

        IMateEntity mate = e.MateEntity;

        if (!mate.IsAlive())
        {
            return;
        }

        long mateXp = _characterAlgorithm.GetLevelXp(mate.Level, true, mate.MateType);
        long experience = e.Experience;

        if (mate.Level >= _serverManager.MaxMateLevel)
        {
            mate.Level = (byte)_serverManager.MaxMateLevel;
            mate.Experience = 0;
            return;
        }

        if (mate.Level > character.Level)
        {
            return;
        }

        if (mate.Level == character.Level && mate.Experience >= mateXp)
        {
            mate.Experience = mateXp;
            session.SendPetInfo(mate, _gameLanguage);
            return;
        }

        mate.Experience += experience;
        session.SendPetInfo(mate, _gameLanguage);

        if (mate.Experience < mateXp)
        {
            return;
        }

        if (mate.Level + 1 > character.Level)
        {
            mate.Experience = mateXp;
            session.SendPetInfo(mate, _gameLanguage);
            return;
        }

        int loyalty = mate.Loyalty + 100 > 1000 ? 1000 - mate.Loyalty : 100;
        mate.Loyalty += (short)loyalty;
        mate.Experience -= mateXp;
        mate.Level++;
        mate.RefreshStatistics();
        session.RefreshParty(_spPartnerConfiguration);
        mate.Hp = mate.MaxHp;
        mate.Mp = mate.MaxMp;
        mate.BroadcastEffectInRange(EffectType.NormalLevelUp);
        mate.BroadcastEffectInRange(EffectType.NormalLevelUpSubEffect);
        string name = mate.MateName == mate.Name ? _gameLanguage.GetLanguage(GameDataType.NpcMonster, mate.Name, session.UserLanguage) : mate.MateName;
        GameDialogKey dialogKey = mate.MateType == MateType.Partner ? GameDialogKey.PARTNER_SHOUTMESSAGE_LEVEL_UP : GameDialogKey.PET_SHOUTMESSAGE_LEVEL_UP;
        session.SendMsg(session.GetLanguageFormat(dialogKey, name), MsgMessageType.Middle);
        session.SendPetInfo(mate, _gameLanguage);

        await session.EmitEventAsync(new LevelUpMateEvent
        {
            Level = mate.Level,
            LevelUpType = MateLevelUpType.Normal,
            Location = new Location(mate.MapInstance.MapId, mate.MapX, mate.MapY),
            NosMateMonsterVnum = mate.NpcMonsterVNum
        });
    }
}