using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhoenixLib.Logging;
using WingsAPI.Communication;
using WingsAPI.Communication.DbServer.CharacterService;
using WingsAPI.Data.Character;
using WingsEmu.Game.Managers;

namespace WingsEmu.Plugins.BasicImplementations.Managers;

public class RankingManager : IRankingManager
{
    private readonly ICharacterService _characterService;

    public RankingManager(ICharacterService characterService) => _characterService = characterService;

    public IReadOnlyList<CharacterDTO> TopCompliment { get; private set; } = new List<CharacterDTO>();
    public IReadOnlyList<CharacterDTO> TopPoints { get; private set; } = new List<CharacterDTO>();
    public IReadOnlyList<CharacterDTO> TopReputation { get; private set; } = new List<CharacterDTO>();

    public async Task TryRefreshRanking()
    {
        CharacterGetTopResponse response = null;
        try
        {
            response = await _characterService.GetTopCompliment(new EmptyRpcRequest());
        }
        catch (Exception e)
        {
            Log.Error("[RANKING_MANAGER][TRY_REFRESH_RANKING] Unexpected error: ", e);
        }

        if (response?.ResponseType == RpcResponseType.SUCCESS)
        {
            TopCompliment = response.Top ?? new List<CharacterDTO>();
        }

        response = null;
        try
        {
            response = await _characterService.GetTopPoints(new EmptyRpcRequest());
        }
        catch (Exception e)
        {
            Log.Error("[RANKING_MANAGER][TRY_REFRESH_RANKING] Unexpected error: ", e);
        }

        if (response?.ResponseType == RpcResponseType.SUCCESS)
        {
            TopPoints = response.Top ?? new List<CharacterDTO>();
        }

        response = null;
        try
        {
            response = await _characterService.GetTopReputation(new EmptyRpcRequest());
        }
        catch (Exception e)
        {
            Log.Error("[RANKING_MANAGER][TRY_REFRESH_RANKING] Unexpected error: ", e);
        }

        if (response?.ResponseType == RpcResponseType.SUCCESS)
        {
            TopReputation = response.Top ?? new List<CharacterDTO>();
        }
    }

    public void RefreshRanking(IReadOnlyList<CharacterDTO> topComplimented, IReadOnlyList<CharacterDTO> topPoints, IReadOnlyList<CharacterDTO> topReputation)
    {
        TopCompliment = topComplimented;
        TopPoints = topPoints;
        TopReputation = topReputation;
    }
}