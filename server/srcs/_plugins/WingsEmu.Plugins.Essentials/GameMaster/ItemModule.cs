using System.Threading.Tasks;
using PhoenixLib.Scheduler;
using Qmmands;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Game.Extensions.PacketGeneration;
using WingsEmu.Commands.Checks;
using WingsEmu.Commands.Entities;
using WingsEmu.DTOs.Account;
using WingsEmu.Game;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Portals;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.Essentials.GameMaster;

[Name("Super Game Master")]
[Description("Module related to items Super Game Master commands.")]
[RequireAuthority(AuthorityType.SuperGameMaster)]
public class ItemModule : SaltyModuleBase
{
    private readonly IGameItemInstanceFactory _gameItemInstanceFactory;
    private readonly IGameLanguageService _language;
    private readonly IPortalFactory _portalFactory;
    private readonly IScheduler _scheduler;

    public ItemModule(IGameLanguageService language, IGameItemInstanceFactory gameItemInstanceFactory, IScheduler scheduler, IPortalFactory portalFactory)
    {
        _language = language;
        _gameItemInstanceFactory = gameItemInstanceFactory;
        _scheduler = scheduler;
        _portalFactory = portalFactory;
    }

    [Command("cportal")]
    [Description("Creates a temporary portal to the place you specify.")]
    public async Task<SaltyCommandResult> CreateAPortal(
        [Description("The Id of the Destination Map.")]
        short mapDestId,
        [Description("The value of the coordinate X where the portal will deploy you.")]
        short mapDestX,
        [Description("The value of the coordinate Y where the portal will deploy you.")]
        short mapDestY,
        [Description("The Portal Duration In Seconds")]
        int timeInSeconds = 0)
    {
        IPortalEntity portal = _portalFactory.CreatePortal(PortalType.TSNormal, Context.Player.CurrentMapInstance, Context.Player.PlayerEntity.Position, mapDestId, new Position(mapDestX, mapDestY));
        Context.Player.CurrentMapInstance.AddPortalToMap(portal, _scheduler, timeInSeconds, timeInSeconds > 0);
        return new SaltyCommandResult(true);
    }

    [Command("rportal", "remove-portal")]
    [Description("Removes the closest temporary portal.")]
    public async Task<SaltyCommandResult> RemovePortal()
    {
        IPortalEntity portalToDelete = Context.Player.CurrentMapInstance.GetClosestPortal(Context.Player.PlayerEntity.PositionX, Context.Player.PlayerEntity.PositionY);

        if (portalToDelete == null)
        {
            return new SaltyCommandResult(false, "There are no temporary portals in your area.");
        }

        if (!Context.Player.PlayerEntity.Position.IsInRange(new Position(portalToDelete.PositionX, portalToDelete.PositionY), 3))
        {
            return new SaltyCommandResult(false, $"You're not close enough to the temporary portal. (X: {portalToDelete.PositionX}; Y: {portalToDelete.PositionY})");
        }

        Context.Player.CurrentMapInstance.DeletePortal(portalToDelete);
        return new SaltyCommandResult(true, "The temporary portal has been removed successfully!");
    }

    [Command("pearl")]
    [Description("Creates a peral with the vnum of the mate you desire.")]
    public async Task<SaltyCommandResult> CreateMatePearl(
        [Description("The VNum of the mate you want.")]
        int item, bool isLimited)
    {
        IClientSession session = Context.Player;

        GameItemInstance itemInstance = _gameItemInstanceFactory.CreateItem(item, isLimited);

        await session.AddNewItemToInventory(itemInstance);
        return new SaltyCommandResult(true);
    }

    [Command("item")]
    [Description("Create an Item")]
    public async Task<SaltyCommandResult> CreateitemAsync(
        [Description("VNUM Item.")] short itemvnum)
    {
        IClientSession session = Context.Player;
        GameItemInstance newItem = _gameItemInstanceFactory.CreateItem(itemvnum);
        await session.AddNewItemToInventory(newItem);
        return new SaltyCommandResult(true, $"Created item: {_language.GetLanguage(GameDataType.Item, newItem.GameItem.Name, Context.Player.UserLanguage)}");
    }

    [Command("fairy-level", "flevel", "flvl", "fairylevel")]
    [Description("Set equipped fairy level.")]
    public async Task<SaltyCommandResult> FairyLevel([Description("Fairy level.")] short fairyLevel)
    {
        IClientSession session = Context.Player;
        if (session.PlayerEntity.Fairy == null || session.PlayerEntity.Fairy.IsEmpty)
        {
            return new SaltyCommandResult(false, "No fairy equipped.");
        }

        session.PlayerEntity.Fairy.ElementRate = fairyLevel;
        session.RefreshFairy();

        return new SaltyCommandResult(true, $"Your fairy level has been set to {fairyLevel}%!");
    }

    [Command("sp")]
    [Description("Create a Specialist Card with upgrade.")]
    public async Task<SaltyCommandResult> SpAsync(
        [Description("SP VNUM.")] short spvnum,
        [Description("Upgrade.")] byte upgrade = 0)
    {
        IClientSession session = Context.Player;

        GameItemInstance newItem = _gameItemInstanceFactory.CreateSpecialistCard(spvnum, upgrade: upgrade);
        await session.AddNewItemToInventory(newItem);
        return new SaltyCommandResult(true, $"Specialist Card: {_language.GetLanguage(GameDataType.Item, newItem.GameItem.Name, session.UserLanguage)} created.");
    }
}