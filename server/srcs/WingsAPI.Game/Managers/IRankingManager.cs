using System.Collections.Generic;
using System.Threading.Tasks;
using WingsAPI.Data.Character;

namespace WingsEmu.Game.Managers;

public interface IRankingManager
{
    IReadOnlyList<CharacterDTO> TopCompliment { get; }
    IReadOnlyList<CharacterDTO> TopPoints { get; }
    IReadOnlyList<CharacterDTO> TopReputation { get; }

    Task TryRefreshRanking();
    void RefreshRanking(IReadOnlyList<CharacterDTO> topComplimented, IReadOnlyList<CharacterDTO> topPoints, IReadOnlyList<CharacterDTO> topReputation);
}