using System;
using System.Collections.Generic;
using Mapster;
using WingsEmu.DTOs.Items;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Skills;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.Inventory;

public class GameItemInstanceFactory : IGameItemInstanceFactory
{
    private readonly IItemsManager _itemsManager;
    private readonly IRandomGenerator _randomGenerator;
    private readonly IDropRarityConfigurationProvider _rarityConfigurationProvider;

    public GameItemInstanceFactory(IItemsManager itemsManager, IRandomGenerator randomGenerator, IDropRarityConfigurationProvider rarityConfigurationProvider)
    {
        _itemsManager = itemsManager;
        _randomGenerator = randomGenerator;
        _rarityConfigurationProvider = rarityConfigurationProvider;
    }

    public GameItemInstance CreateItem(ItemInstanceDTO dto)
    {
        GameItemInstance instance = dto.Adapt<GameItemInstance>();
        if (instance.SerialTracker == null && _itemsManager.GetItem(instance.ItemVNum)?.IsNotStackableInventoryType() == true)
        {
            instance.SerialTracker = Guid.NewGuid();
        }

        return instance;
    }

    public ItemInstanceDTO CreateDto(GameItemInstance instance)
    {
        ItemInstanceDTO dto = instance.Adapt<ItemInstanceDTO>();
        if (dto.SerialTracker == null && _itemsManager.GetItem(dto.ItemVNum)?.IsNotStackableInventoryType() == true)
        {
            dto.SerialTracker = Guid.NewGuid();
        }

        return dto;
    }

    public GameItemInstance CreateItem(int itemVnum) => CreateItem(itemVnum, 1, 0, 0, 0);
    public GameItemInstance CreateItem(int itemVnum, bool isMateLimited) => CreateItem(itemVnum, 1, 0, 0, 0, isMateLimited);

    public GameItemInstance CreateItem(int itemVnum, int amount) => CreateItem(itemVnum, amount, 0, 0, 0);

    public GameItemInstance CreateItem(int itemVnum, int amount, byte upgrade) => CreateItem(itemVnum, amount, upgrade, 0, 0);
    public GameItemInstance CreateItem(int itemVnum, int amount, byte upgrade, sbyte rare) => CreateItem(itemVnum, amount, upgrade, rare, 0);

    public GameItemInstance CreateItem(int itemVnum, int amount, byte upgrade, sbyte rare, byte design, bool isMateLimited = false)
    {
        IGameItem newGameItem = _itemsManager.GetItem(itemVnum);
        if (newGameItem == null)
        {
            return null;
        }

        bool isNotStackable = newGameItem.IsNotStackableInventoryType();

        if (amount > 999 && itemVnum != (int)ItemVnums.GOLD)
        {
            amount = 999;
        }

        if (isNotStackable && amount != 1)
        {
            amount = 1;
        }

        switch (newGameItem.ItemType)
        {
            case ItemType.Shell:
                return new GameItemInstance
                {
                    Type = ItemInstanceType.WearableInstance,
                    ItemVNum = itemVnum,
                    Amount = amount,
                    Upgrade = upgrade == 0 ? (byte)_randomGenerator.RandomNumber(newGameItem.ShellMinimumLevel, newGameItem.ShellMaximumLevel) : upgrade,
                    Rarity = rare == 0 ? _rarityConfigurationProvider.GetRandomRarity(ItemType.Shell) : rare,
                    Design = design,
                    DurabilityPoint = newGameItem.LeftUsages,
                    EquipmentOptions = new List<EquipmentOptionDTO>()
                };
            case ItemType.Weapon:
            case ItemType.Armor:
            case ItemType.Fashion:
            case ItemType.Jewelry:
                var item = new GameItemInstance
                {
                    Type = ItemInstanceType.WearableInstance,
                    ItemVNum = itemVnum,
                    Amount = amount,
                    Upgrade = upgrade,
                    Rarity = rare,
                    Design = design,
                    DurabilityPoint = newGameItem.LeftUsages,
                    EquipmentOptions = new List<EquipmentOptionDTO>()
                };
                if (item.Rarity != 0)
                {
                    item.SetRarityPoint(_randomGenerator);
                }

                return item;
            case ItemType.Specialist:
                if (newGameItem.IsPartnerSpecialist)
                {
                    return new GameItemInstance
                    {
                        Type = ItemInstanceType.SpecialistInstance,
                        ItemVNum = itemVnum,
                        Amount = amount,
                        Agility = 0,
                        PartnerSkills = new List<PartnerSkill>()
                    };
                }

                return new GameItemInstance
                {
                    Type = ItemInstanceType.SpecialistInstance,
                    ItemVNum = itemVnum,
                    Amount = amount,
                    SpLevel = 1,
                    Upgrade = upgrade
                };
            case ItemType.Box:
                byte level = newGameItem.ItemSubType switch
                {
                    0 => (byte)newGameItem.Data[2],
                    1 => (byte)newGameItem.Data[2],
                    _ => 1
                };

                return new GameItemInstance
                {
                    Type = ItemInstanceType.BoxInstance,
                    ItemVNum = itemVnum,
                    HoldingVNum = newGameItem.Data[1],
                    Amount = amount,
                    Rarity = rare,
                    Design = design,
                    SpLevel = level,
                    IsLimitedMatePearl = isMateLimited
                };
        }

        return new GameItemInstance(itemVnum, amount, upgrade, rare, design);
    }

    public GameItemInstance CreateSpecialistCard(int itemVnum, byte spLevel = 1, byte upgrade = 0, byte design = 0)
    {
        IGameItem newGameItem = _itemsManager.GetItem(itemVnum);
        if (newGameItem == null)
        {
            return null;
        }

        return new GameItemInstance
        {
            Type = ItemInstanceType.SpecialistInstance,
            ItemVNum = itemVnum,
            Amount = 1,
            SpLevel = spLevel,
            Upgrade = upgrade,
            Design = design
        };
    }

    public GameItemInstance DuplicateItem(GameItemInstance gameInstance) => gameInstance.Adapt<GameItemInstance>();
}