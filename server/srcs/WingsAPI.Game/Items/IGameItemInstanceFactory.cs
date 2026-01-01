using WingsEmu.DTOs.Items;

namespace WingsEmu.Game.Items;

public interface IGameItemInstanceFactory
{
    GameItemInstance CreateItem(ItemInstanceDTO dto);
    ItemInstanceDTO CreateDto(GameItemInstance instance);

    GameItemInstance CreateItem(int itemVnum);
    GameItemInstance CreateItem(int itemVnum, bool isMateLimited);
    GameItemInstance CreateItem(int itemVnum, int amount);
    GameItemInstance CreateItem(int itemVnum, int amount, byte upgrade);
    GameItemInstance CreateItem(int itemVnum, int amount, byte upgrade, sbyte rare);
    GameItemInstance CreateItem(int itemVnum, int amount, byte upgrade, sbyte rare, byte design, bool isMateLimited = false);

    GameItemInstance CreateSpecialistCard(int itemVnum, byte spLevel = 1, byte upgrade = 0, byte design = 0);
    GameItemInstance DuplicateItem(GameItemInstance gameInstance);
}