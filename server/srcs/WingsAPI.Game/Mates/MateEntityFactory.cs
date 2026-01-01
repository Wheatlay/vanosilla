using PhoenixLib.Events;
using WingsEmu.DTOs.Mates;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Npcs;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Game.Mates;

public class MateEntityFactory : IMateEntityFactory
{
    private readonly IBattleEntityAlgorithmService _algorithm;
    private readonly IAsyncEventPipeline _eventPipeline;
    private readonly IMateTransportFactory _mateTransportFactory;
    private readonly INpcMonsterManager _npcMonsterManager;
    private readonly IRandomGenerator _randomGenerator;

    public MateEntityFactory(INpcMonsterManager npcMonsterManager, IAsyncEventPipeline eventPipeline, IBattleEntityAlgorithmService algorithm, IRandomGenerator randomGenerator,
        IMateTransportFactory mateTransportFactory)
    {
        _npcMonsterManager = npcMonsterManager;
        _eventPipeline = eventPipeline;
        _algorithm = algorithm;
        _randomGenerator = randomGenerator;
        _mateTransportFactory = mateTransportFactory;
    }

    public IMateEntity CreateMateEntity(IPlayerEntity owner, MonsterData monsterData, MateType mateType) => CreateMateEntity(owner, monsterData, mateType, 1);

    public IMateEntity CreateMateEntity(IPlayerEntity playerEntity, MateDTO mateDto)
    {
        var monsterData = new MonsterData(_npcMonsterManager.GetNpc(mateDto.NpcMonsterVNum));
        var mate = new MateEntity(playerEntity, monsterData, mateDto.Level, mateDto.MateType, _mateTransportFactory, _eventPipeline, _algorithm, _randomGenerator)
        {
            Attack = mateDto.Attack,
            CanPickUp = mateDto.CanPickUp,
            CharacterId = mateDto.CharacterId,
            Defence = mateDto.Defence,
            Direction = mateDto.Direction,
            Experience = mateDto.Experience,
            Hp = mateDto.Hp,
            Level = mateDto.Level,
            Loyalty = mateDto.Loyalty,
            Mp = mateDto.Mp,
            MateName = mateDto.MateName,
            Skin = mateDto.Skin,
            IsSummonable = mateDto.IsSummonable,
            MapX = mateDto.MapX,
            MapY = mateDto.MapY,
            MateType = mateDto.MateType,
            PetSlot = mateDto.PetSlot,
            MinilandX = mateDto.MinilandX,
            MinilandY = mateDto.MinilandY,
            IsTeamMember = mateDto.IsTeamMember,
            IsLimited = mateDto.IsLimited
        };

        return mate;
    }

    public IMateEntity CreateMateEntity(IPlayerEntity owner, int monsterVnum, MateType mateType)
    {
        IMonsterData monsterData = _npcMonsterManager.GetNpc(monsterVnum);
        return monsterData == null ? null : CreateMateEntity(owner, new MonsterData(monsterData), mateType, 1);
    }

    public IMateEntity CreateMateEntity(IPlayerEntity owner, MonsterData monsterData, MateType mateType, byte level) => CreateMateEntity(owner, monsterData, mateType, level, false);

    public IMateEntity CreateMateEntity(IPlayerEntity owner, MonsterData monsterData, MateType mateType, byte level, bool isLimited) =>
        new MateEntity(owner, monsterData, level, mateType, _mateTransportFactory, _eventPipeline, _algorithm, _randomGenerator)
        {
            IsLimited = isLimited
        };

    public MateDTO CreateMateDto(IMateEntity mateEntity) => new()
    {
        Id = mateEntity.Id,
        Attack = mateEntity.Attack,
        CanPickUp = mateEntity.CanPickUp,
        CharacterId = mateEntity.CharacterId,
        Defence = mateEntity.Defence,
        Direction = mateEntity.Direction,
        Experience = mateEntity.Experience,
        Hp = mateEntity.Hp,
        Level = mateEntity.Level,
        Loyalty = mateEntity.Loyalty,
        Mp = mateEntity.Mp,
        MateName = mateEntity.MateName,
        Skin = mateEntity.Skin,
        IsSummonable = mateEntity.IsSummonable,
        MapX = mateEntity.MapX,
        MapY = mateEntity.MapY,
        MateType = mateEntity.MateType,
        PetSlot = mateEntity.PetSlot,
        MinilandX = mateEntity.MinilandX,
        MinilandY = mateEntity.MinilandY,
        IsTeamMember = mateEntity.IsTeamMember,
        NpcMonsterVNum = mateEntity.NpcMonsterVNum,
        IsLimited = mateEntity.IsLimited
    };
}