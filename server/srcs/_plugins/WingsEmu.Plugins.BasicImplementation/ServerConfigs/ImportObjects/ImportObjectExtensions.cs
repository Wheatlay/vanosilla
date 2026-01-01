using System.Collections.Generic;
using System.Linq;
using WingsAPI.Data.Drops;
using WingsAPI.Data.Shops;
using WingsEmu.DTOs.Maps;
using WingsEmu.DTOs.Recipes;
using WingsEmu.DTOs.ServerDatas;
using WingsEmu.DTOs.Shops;
using WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Drops;
using WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.ItemBoxes;
using WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Maps;
using WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Monsters;
using WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Npcs;
using WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Portals;
using WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Recipes;
using WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Teleporters;

namespace WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects;

public static class ImportObjectExtensions
{
    public static List<DropDTO> ToDto(this DropObject obj)
    {
        var drops = new List<DropDTO>();

        if (obj.MonsterVnums != null)
        {
            drops.AddRange(obj.MonsterVnums.Select(vnum => new DropDTO
            {
                Amount = obj.Quantity,
                DropChance = obj.Chance,
                ItemVNum = obj.ItemVNum,
                MonsterVNum = vnum
            }));
        }

        if (obj.MapIds != null)
        {
            drops.AddRange(obj.MapIds.Select(mapId => new DropDTO
            {
                Amount = obj.Quantity,
                DropChance = obj.Chance,
                ItemVNum = obj.ItemVNum,
                MapId = mapId
            }));
        }

        if (obj.Races != null)
        {
            drops.AddRange(obj.Races.Select(raceDrop => new DropDTO
            {
                Amount = obj.Quantity,
                DropChance = obj.Chance,
                ItemVNum = obj.ItemVNum,
                RaceType = raceDrop[0],
                RaceSubType = raceDrop[1]
            }));
        }

        if (obj.MonsterVnums?.Any() != false || obj.MapIds?.Any() != false || obj.Races?.Any() != false)
        {
            return drops;
        }

        drops.Add(new DropDTO
        {
            Amount = obj.Quantity,
            DropChance = obj.Chance,
            ItemVNum = obj.ItemVNum
        });

        return drops;
    }

    public static PortalDTO ToDto(this PortalObject portal) =>
        new PortalDTO
        {
            Type = portal.Type,
            DestinationMapId = portal.DestinationMapId,
            DestinationX = portal.DestinationX,
            DestinationY = portal.DestinationY,
            IsDisabled = portal.IsDisabled,
            SourceX = portal.SourceX,
            SourceY = portal.SourceY,
            SourceMapId = portal.SourceMapId,
            RaidType = portal.RaidType,
            MapNameId = portal.MapNameId
        };

    public static ServerMapDto ToDto(this ConfiguredMapObject toDto) =>
        new ServerMapDto
        {
            Id = toDto.MapId,
            AmbientId = toDto.AmbientId,
            MapVnum = toDto.MapVnum,
            MusicId = toDto.MusicId,
            NameId = toDto.NameId,
            Flags = toDto.Flags ?? new List<MapFlags>()
        };

    public static ItemBoxDto ToDtos(this RandomBoxObject obj)
    {
        if (obj.Categories.Count < 0)
        {
            return null;
        }

        var list = new List<ItemBoxItemDto>();

        int i = 0;

        foreach (RandomBoxCategory category in obj.Categories)
        {
            if (category.Items == null || !category.Items.Any())
            {
                continue;
            }

            i++;

            foreach (RandomBoxItem item in category.Items)
            {
                list.Add(new ItemBoxItemDto
                {
                    ItemGeneratedAmount = (short)item.Quantity,
                    ItemGeneratedUpgrade = item.Upgrade,
                    ItemGeneratedVNum = (short)item.ItemVnum,
                    ItemGeneratedRandomRarity = item.RandomRarity,
                    MaximumOriginalItemRare = item.MaximumRandomRarity,
                    MinimumOriginalItemRare = item.MinimumRandomRarity,
                    Probability = (short)category.Chance
                });
            }
        }

        return new ItemBoxDto
        {
            Id = obj.ItemVnum,
            MinimumRewards = obj.MinimumRewards,
            MaximumRewards = obj.MaximumRewards,
            ItemBoxType = ItemBoxType.RANDOM_PICK,
            ShowsRaidBoxPanelOnOpen = obj.HideRewardInfo == false,
            Items = list
        };
    }

    public static ItemBoxDto ToDto(this ItemBoxImportFile obj)
    {
        var list = new List<ItemBoxItemDto>();

        if (obj.Items.Count == 0)
        {
            return null;
        }

        foreach (RandomBoxItem item in obj.Items)
        {
            list.Add(new ItemBoxItemDto
            {
                ItemGeneratedAmount = (short)item.Quantity,
                ItemGeneratedUpgrade = item.Upgrade,
                ItemGeneratedVNum = (short)item.ItemVnum,
                ItemGeneratedRandomRarity = item.RandomRarity,
                MaximumOriginalItemRare = item.MaximumRandomRarity,
                MinimumOriginalItemRare = item.MinimumRandomRarity
            });
        }

        return new ItemBoxDto
        {
            Id = obj.ItemVnum,
            MinimumRewards = obj.MinimumRewards,
            MaximumRewards = obj.MaximumRewards,
            ItemBoxType = ItemBoxType.BUNDLE,
            ShowsRaidBoxPanelOnOpen = obj.ShowRaidBoxModalOnOpen,
            Items = list
        };
    }

    public static ShopSkillDTO ToDto(this MapNpcShopSkillObject obj, byte tabId, short position) => new()
    {
        SkillVNum = obj.SkillVnum,
        Type = tabId,
        Slot = position
    };


    public static RecipeDTO ToDto(this RecipeObject obj) => new()
    {
        ProducedItemVnum = obj.ItemVnum,
        Amount = obj.Quantity,
        ProducerNpcVnum = obj.ProducerNpcVnum,
        ProducerMapNpcId = obj.ProducerMapNpcId,
        ProducerItemVnum = obj.ProducerItemVnum
    };

    public static RecipeItemDTO ToDto(this RecipeItemObject obj, short slot) => new()
    {
        ItemVNum = obj.ItemVnum,
        Slot = slot,
        Amount = obj.Quantity
    };

    public static ShopItemDTO ToDto(this MapNpcShopItemObject obj, byte shopType, short position) =>
        new()
        {
            Type = shopType,
            ItemVNum = (short)obj.ItemVnum,
            Rare = obj.Rarity,
            Slot = position,
            Upgrade = obj.Upgrade,
            Price = obj.Price,
            Color = obj.Design
        };

    public static ShopDTO ToDto<T>(this MapNpcShopObject<T> shop) =>
        new()
        {
            Name = shop.Name,
            MenuType = shop.MenuType,
            ShopType = shop.ShopType
        };

    public static MapNpcDTO ToDto(this MapNpcObject obj) =>
        new()
        {
            Id = obj.MapNpcId,
            NpcVNum = (short)obj.NpcMonsterVnum,
            MapX = obj.PosX,
            MapY = obj.PosY,
            MapId = obj.MapId,
            Dialog = (short)obj.DialogId,
            QuestDialog = obj.QuestDialog,
            Effect = (short)obj.Effect,
            EffectDelay = (short)obj.EffectDelay,
            Direction = obj.Direction,
            IsMoving = obj.IsMoving,
            IsSitting = obj.IsSitting,
            IsDisabled = obj.IsDisabled,
            CanAttack = obj.CanAttack,
            HasGodMode = obj.HasGodMode,
            CustomName = obj.CustomName
        };


    public static TeleporterDTO ToDto(this TeleporterObject teleporterObject) => new()
    {
        Index = teleporterObject.Index,
        Type = teleporterObject.Type,
        MapId = teleporterObject.MapId,
        MapNpcId = teleporterObject.MapNpcId,
        MapX = teleporterObject.MapX,
        MapY = teleporterObject.MapY
    };

    public static MapMonsterDTO ToDto(this MapMonsterObject obj) => new()
    {
        MapId = obj.MapId,
        MonsterVNum = obj.MonsterVNum,
        Direction = obj.Position,
        MapX = obj.MapX,
        MapY = obj.MapY,
        IsMoving = obj.IsMoving
    };
}