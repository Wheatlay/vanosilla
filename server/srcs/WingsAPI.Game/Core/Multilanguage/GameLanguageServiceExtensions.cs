using WingsEmu.DTOs.Buffs;
using WingsEmu.DTOs.Skills;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;

namespace WingsEmu.Game._i18n;

public static class GameLanguageServiceExtensions
{
    public static string GetItemName(this IGameLanguageService gameLanguage, IGameItem item, IClientSession session) => gameLanguage.GetLanguage(GameDataType.Item, item.Name, session.UserLanguage);

    public static string GetNpcMonsterName(this IGameLanguageService gameLanguage, IMonsterData item, IClientSession session) =>
        gameLanguage.GetLanguage(GameDataType.NpcMonster, item.Name, session.UserLanguage);

    public static string GetSkillName(this IGameLanguageService gameLanguage, SkillDTO item, IClientSession session) => gameLanguage.GetLanguage(GameDataType.Skill, item.Name, session.UserLanguage);

    public static string GetCardName(this IGameLanguageService gameLanguage, CardDTO item, IClientSession session) => gameLanguage.GetLanguage(GameDataType.Card, item.Name, session.UserLanguage);
}