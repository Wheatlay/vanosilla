using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhoenixLib.Logging;
using WingsAPI.Data.Character;

namespace DatabaseServer.Managers
{
    public class RankingManager : IRankingManager
    {
        private readonly ICharacterDAO _characterDao;

        private IReadOnlyList<CharacterDTO> _topCompliment;
        private IReadOnlyList<CharacterDTO> _topPoints;
        private IReadOnlyList<CharacterDTO> _topReputation;

        public RankingManager(ICharacterDAO characterDao) => _characterDao = characterDao;

        public async Task<IReadOnlyList<CharacterDTO>> GetTopCompliment()
        {
            if (_topCompliment != null)
            {
                return _topCompliment;
            }

            try
            {
                _topCompliment = await _characterDao.GetTopCompliment();
            }
            catch (Exception e)
            {
                Log.Error("[RANKING_MANAGER][GET_TOP_COMPLIMENT] Unexpected error:", e);
            }

            return _topCompliment;
        }

        public async Task<IReadOnlyList<CharacterDTO>> GetTopPoints()
        {
            if (_topPoints != null)
            {
                return _topPoints;
            }

            try
            {
                _topPoints = await _characterDao.GetTopPoints();
            }
            catch (Exception e)
            {
                Log.Error("[RANKING_MANAGER][GET_TOP_POINTS] Unexpected error:", e);
            }

            return _topPoints;
        }

        public async Task<IReadOnlyList<CharacterDTO>> GetTopReputation()
        {
            if (_topReputation != null)
            {
                return _topReputation;
            }

            try
            {
                _topReputation = await _characterDao.GetTopReputation();
            }
            catch (Exception e)
            {
                Log.Error("[RANKING_MANAGER][GET_TOP_REPUTATION] Unexpected error:", e);
            }

            return _topReputation;
        }

        public async Task<RefreshResponse> TryRefreshRanking()
        {
            try
            {
                _topCompliment = await _characterDao.GetTopCompliment();
            }
            catch (Exception e)
            {
                Log.Error("[RANKING_MANAGER][TRY_REFRESH_RANKING] Unexpected error:", e);
                return new RefreshResponse
                {
                    Success = false
                };
            }

            try
            {
                _topPoints = await _characterDao.GetTopPoints();
            }
            catch (Exception e)
            {
                Log.Error("[RANKING_MANAGER][TRY_REFRESH_RANKING] Unexpected error:", e);
                return new RefreshResponse
                {
                    Success = false
                };
            }

            try
            {
                _topReputation = await _characterDao.GetTopReputation();
            }
            catch (Exception e)
            {
                Log.Error("[RANKING_MANAGER][TRY_REFRESH_RANKING] Unexpected error:", e);
                return new RefreshResponse
                {
                    Success = false
                };
            }

            return new RefreshResponse
            {
                Success = true,
                TopCompliment = _topCompliment,
                TopPoints = _topPoints,
                TopReputation = _topReputation
            };
        }
    }
}