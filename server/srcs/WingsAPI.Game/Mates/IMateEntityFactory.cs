using WingsEmu.DTOs.Mates;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Npcs;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Game.Mates;

public interface IMateEntityFactory
{
    public IMateEntity CreateMateEntity(IPlayerEntity playerEntity, MateDTO mateDto);
    public IMateEntity CreateMateEntity(IPlayerEntity owner, int monsterVnum, MateType mateType);
    public IMateEntity CreateMateEntity(IPlayerEntity owner, MonsterData monsterData, MateType mateType);
    public IMateEntity CreateMateEntity(IPlayerEntity owner, MonsterData monsterData, MateType mateType, byte level);
    public IMateEntity CreateMateEntity(IPlayerEntity owner, MonsterData monsterData, MateType mateType, byte level, bool isLimited);

    public MateDTO CreateMateDto(IMateEntity mateEntity);
}