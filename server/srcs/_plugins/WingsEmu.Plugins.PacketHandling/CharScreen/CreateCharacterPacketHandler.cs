// WingsEmu
// 
// Developed by NosWings Team

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CloneExtensions;
using Mapster;
using PhoenixLib.Logging;
using WingsAPI.Communication;
using WingsAPI.Communication.DbServer.CharacterService;
using WingsAPI.Data.Character;
using WingsEmu.Customization.NewCharCustomisation;
using WingsEmu.DTOs.Account;
using WingsEmu.DTOs.Inventory;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums;
using WingsEmu.Plugins.PacketHandling.Customization;

namespace WingsEmu.Plugins.PacketHandling.CharScreen;

public class CreateCharacterPacketHandler : GenericCharScreenPacketHandlerBase<CharacterCreatePacket>
{
    private readonly BaseCharacter _baseCharacter;
    private readonly BaseInventory _baseInventory;
    private readonly BaseQuicklist _baseQuicklist;
    private readonly BaseSkill _baseSkill;
    private readonly ICharacterService _characterService;
    private readonly EntryPointPacketHandler _entrypoint;
    private readonly IForbiddenNamesManager _forbiddenNamesManager;
    private readonly IGameItemInstanceFactory _gameItemInstanceFactory;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IGameItemInstanceFactory _itemInstanceFactory;
    private readonly IMapManager _mapManager;
    private readonly IRandomGenerator _randomGenerator;
    private readonly IRespawnDefaultConfiguration _respawnDefaultConfiguration;

    public CreateCharacterPacketHandler(EntryPointPacketHandler entrypoint, IGameLanguageService gameLanguage, BaseCharacter baseCharacter, BaseSkill baseSkill, BaseQuicklist baseQuicklist,
        BaseInventory baseInventory, IGameItemInstanceFactory gameItemInstanceFactory, ICharacterService characterService, IMapManager mapManager,
        IRespawnDefaultConfiguration respawnDefaultConfiguration, IRandomGenerator randomGenerator, IGameItemInstanceFactory itemInstanceFactory, IForbiddenNamesManager forbiddenNamesManager)
    {
        _entrypoint = entrypoint;
        _gameLanguage = gameLanguage;
        _baseCharacter = baseCharacter;
        _baseSkill = baseSkill;
        _baseQuicklist = baseQuicklist;
        _baseInventory = baseInventory;
        _gameItemInstanceFactory = gameItemInstanceFactory;
        _characterService = characterService;
        _mapManager = mapManager;
        _respawnDefaultConfiguration = respawnDefaultConfiguration;
        _randomGenerator = randomGenerator;
        _itemInstanceFactory = itemInstanceFactory;
        _forbiddenNamesManager = forbiddenNamesManager;
    }

    protected override async Task HandlePacketAsync(IClientSession session, CharacterCreatePacket packet)
    {
        if (session.HasCurrentMapInstance)
        {
            Log.Warn("HAS_CURRENTMAP_INSTANCE");
            return;
        }

        // TODO: Hold Account Information in Authorized object
        long accountId = session.Account.Id;
        byte slot = packet.Slot;
        string characterName = packet.Name;
        DbServerGetCharacterResponse response = await _characterService.GetCharacterBySlot(new DbServerGetCharacterFromSlotRequest
        {
            AccountId = accountId,
            Slot = slot
        });

        if (response.RpcResponseType == RpcResponseType.SUCCESS)
        {
            Log.Warn($"[CREATE_CHARACTER_PACKET_HANDLER] Character slot is already busy. Slot: '{slot.ToString()}'");
            return;
        }

        if (slot > 3)
        {
            Log.Info("SLOTS > 3");
            return;
        }

        if (characterName.Length is < 3 or >= 15 && session.Account.Authority < AuthorityType.GameMaster)
        {
            session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.CHARACTER_CREATION_INFO_INVALID_CHARNAME, session.UserLanguage));
            return;
        }

        if ((byte)packet.HairColor > 9)
        {
            Log.Info("COLOR NOT VALID FOR A NEW CHARACTER");
            return;
        }

        if (packet.HairStyle != HairStyleType.A && packet.HairStyle != HairStyleType.B)
        {
            Log.Info("HAIRSTYLE NOT VALID FOR A NEW CHARACTER");
            return;
        }

        var rg = new Regex(@"^[a-zA-Z0-9_\-\*]*$");
        if (rg.Matches(characterName).Count != 1)
        {
            session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.CHARACTER_CREATION_INFO_INVALID_CHARNAME, session.UserLanguage));
            return;
        }

        if (session.Account.Authority <= AuthorityType.GameMaster)
        {
            string lowerCharName = characterName.ToLower();
            if (_forbiddenNamesManager.IsBanned(lowerCharName, out string bannedName))
            {
                session.SendInfo(_gameLanguage.GetLanguageFormat(GameDialogKey.CHARACTER_CREATION_INFO_BANNED_CHARNAME, session.UserLanguage, bannedName));
                return;
            }
        }

        DbServerGetCharacterResponse response2 = await _characterService.GetCharacterByName(new DbServerGetCharacterRequestByName
        {
            CharacterName = characterName
        });

        if (response2.RpcResponseType == RpcResponseType.SUCCESS)
        {
            session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.CHARACTER_CREATION_INFO_ALREADY_TAKEN, session.UserLanguage));
            return;
        }

        CharacterDTO newCharacter = _baseCharacter.GetCharacter();

        newCharacter.AccountId = accountId;
        newCharacter.Gender = packet.Gender;
        newCharacter.HairColor = packet.HairColor;
        newCharacter.HairStyle = packet.HairStyle;
        newCharacter.Name = characterName;
        newCharacter.Slot = slot;
        newCharacter.QuickGetUp = true;
        newCharacter.UiBlocked = true;
        newCharacter.IsPartnerAutoRelive = true;
        newCharacter.IsPetAutoRelive = true;
        newCharacter.Dignity = 100;
        newCharacter.MinilandPoint = 2000;

        RespawnDefault getRespawn = _respawnDefaultConfiguration.GetReturn(RespawnType.NOSVILLE_SPAWN);
        if (getRespawn != null)
        {
            IMapInstance mapInstance = _mapManager.GetBaseMapInstanceByMapId(getRespawn.MapId);
            if (mapInstance != null)
            {
                int randomX = getRespawn.MapX + _randomGenerator.RandomNumber(getRespawn.Radius, -getRespawn.Radius);
                int randomY = getRespawn.MapY + _randomGenerator.RandomNumber(getRespawn.Radius, -getRespawn.Radius);

                if (mapInstance.IsBlockedZone(randomX, randomY))
                {
                    randomX = getRespawn.MapX;
                    randomY = getRespawn.MapY;
                }

                newCharacter.MapX = (short)randomX;
                newCharacter.MapY = (short)randomY;
            }
        }

        BaseSkill skills = _baseSkill.GetClone();
        if (skills != null)
        {
            newCharacter.LearnedSkills.AddRange(skills.Skills);
        }

        BaseQuicklist quicklist = _baseQuicklist.GetClone();
        if (quicklist != null)
        {
            newCharacter.Quicklist.AddRange(quicklist.Quicklist);
        }

        BaseInventory startupInventory = _baseInventory.GetClone();
        var listOfItems = new List<InventoryItem>();
        if (startupInventory != null)
        {
            foreach (BaseInventory.StartupInventoryItem item in startupInventory.Items)
            {
                GameItemInstance newItem = _gameItemInstanceFactory.CreateItem(item.Vnum, item.Quantity);
                var inventoryItem = new InventoryItem
                {
                    InventoryType = item.InventoryType,
                    IsEquipped = false,
                    ItemInstance = newItem,
                    CharacterId = newCharacter.Id,
                    Slot = item.Slot
                };

                listOfItems.Add(inventoryItem);
            }
        }

        newCharacter.EquippedStuffs = listOfItems.Where(s => s is { IsEquipped: true }).Select(s =>
        {
            CharacterInventoryItemDto tmp = s.Adapt<CharacterInventoryItemDto>();
            tmp.ItemInstance = _itemInstanceFactory.CreateDto(s.ItemInstance);
            return tmp;
        }).ToList();
        newCharacter.Inventory = listOfItems.Where(s => s is { IsEquipped: false }).Select(s =>
        {
            CharacterInventoryItemDto tmp = s.Adapt<CharacterInventoryItemDto>();
            tmp.ItemInstance = _itemInstanceFactory.CreateDto(s.ItemInstance);
            return tmp;
        }).ToList();
        newCharacter.LifetimeStats = new CharacterLifetimeStatsDto();

        DbServerSaveCharacterResponse response3 = await _characterService.CreateCharacter(new DbServerSaveCharacterRequest
        {
            Character = newCharacter
        });

        if (response3.RpcResponseType != RpcResponseType.SUCCESS)
        {
            session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.CHARACTER_CREATION_INFO_ALREADY_TAKEN, session.UserLanguage));
            return;
        }

        await _entrypoint.EntryPointAsync(session, null);
    }
}