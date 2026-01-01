using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhoenixLib.Caching;
using PhoenixLib.Logging;
using WingsAPI.Data.Families;

namespace FamilyServer.Managers
{
    public class FamilyMembershipManager
    {
        private readonly ILongKeyCachedRepository<FamilyMembershipDto> _cachedMembers;
        private readonly ILongKeyCachedRepository<Dictionary<long, FamilyMembershipDto>> _cachedMembersByFamilyId;
        private readonly IFamilyMembershipDao _familyMembershipDao;

        public FamilyMembershipManager(IFamilyMembershipDao familyMembershipDao, ILongKeyCachedRepository<FamilyMembershipDto> cachedMembers,
            ILongKeyCachedRepository<Dictionary<long, FamilyMembershipDto>> cachedMembersByFamilyId)
        {
            _familyMembershipDao = familyMembershipDao;
            _cachedMembers = cachedMembers;
            _cachedMembersByFamilyId = cachedMembersByFamilyId;
        }

        public async Task<FamilyMembershipDto> GetFamilyMembershipByCharacterIdAsync(long characterId)
        {
            FamilyMembershipDto cachedMembership = _cachedMembers.Get(characterId);
            return cachedMembership ?? await _familyMembershipDao.GetByCharacterIdAsync(characterId);
        }

        public async Task<List<FamilyMembershipDto>> GetFamilyMembershipsByFamilyIdAsync(long familyId)
        {
            var list = _cachedMembersByFamilyId.Get(familyId)?.Values.ToList();

            if (list != null)
            {
                return list;
            }

            list = await _familyMembershipDao.GetByFamilyIdAsync(familyId);

            if (list == null || list.Count < 1)
            {
                return null;
            }

            var dictionary = new Dictionary<long, FamilyMembershipDto>();
            foreach (FamilyMembershipDto membership in list)
            {
                if (!dictionary.TryAdd(membership.CharacterId, membership))
                {
                    Log.Warn(
                        $"[FAMILY_MEMBERSHIP_CACHE_MANAGER] Found a duplicated membership for CharacterId: {membership.CharacterId.ToString()} | Duplicated Membership's Id: {membership.Id.ToString()}");
                }

                _cachedMembers.Set(membership.CharacterId, membership);
            }

            _cachedMembersByFamilyId.Set(familyId, dictionary);

            return list;
        }

        public async Task<FamilyMembershipDto> AddFamilyMembershipAsync(FamilyMembershipDto membership)
        {
            FamilyMembershipDto savedMembership = await SaveFamilyMembershipAsync(membership);
            _cachedMembers.Set(savedMembership.CharacterId, savedMembership);
            _cachedMembersByFamilyId.Get(savedMembership.FamilyId)?.Add(savedMembership.CharacterId, savedMembership);
            return savedMembership;
        }

        public async Task<IEnumerable<FamilyMembershipDto>> AddFamilyMembershipsAsync(IReadOnlyList<FamilyMembershipDto> memberships)
        {
            IEnumerable<FamilyMembershipDto> savedMemberships = await SaveFamilyMembershipsAsync(memberships);
            foreach (FamilyMembershipDto membership in savedMemberships)
            {
                _cachedMembers.Set(membership.CharacterId, membership);
                _cachedMembersByFamilyId.Get(membership.FamilyId)?.Add(membership.CharacterId, membership);
            }

            return savedMemberships;
        }

        public async Task<FamilyMembershipDto> SaveFamilyMembershipAsync(FamilyMembershipDto membership) => await _familyMembershipDao.SaveAsync(membership);

        public async Task<IEnumerable<FamilyMembershipDto>> SaveFamilyMembershipsAsync(IReadOnlyList<FamilyMembershipDto> memberships) => await _familyMembershipDao.SaveAsync(memberships);

        public async Task RemoveFamilyMembershipByCharAndFamIdAsync(FamilyMembershipDto familyMembership)
        {
            await _familyMembershipDao.DeleteByIdAsync(familyMembership.Id);
            _cachedMembers.Remove(familyMembership.CharacterId);
            _cachedMembersByFamilyId.Get(familyMembership.FamilyId)?.Remove(familyMembership.CharacterId);
        }
    }
}