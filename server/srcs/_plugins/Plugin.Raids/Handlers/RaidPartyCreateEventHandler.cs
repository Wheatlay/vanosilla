// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using Plugin.Raids.Extension;
using Plugin.Raids.Scripting;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Game.Extensions.ItemExtension.Item;
using WingsAPI.Packets.Enums;
using WingsAPI.Scripting.Object.Raid;
using WingsEmu.DTOs.Maps;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Raids;
using WingsEmu.Game.Raids.Events;
using WingsEmu.Packets.Enums.Chat;

namespace Plugin.Raids;

public class RaidPartyCreateEventHandler : IAsyncEventProcessor<RaidPartyCreateEvent>
{
    private readonly IGameLanguageService _gameLanguage;
    private readonly IItemsManager _itemsManager;
    private readonly IRaidManager _raidManager;
    private readonly RaidScriptManager _raidScriptManager;

    public RaidPartyCreateEventHandler(RaidScriptManager raidScriptManager, IGameLanguageService gameLanguage, IRaidManager raidManager, IItemsManager itemsManager)
    {
        _raidScriptManager = raidScriptManager;
        _gameLanguage = gameLanguage;
        _raidManager = raidManager;
        _itemsManager = itemsManager;
    }

    public async Task HandleAsync(RaidPartyCreateEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        if (session.PlayerEntity.IsInRaidParty)
        {
            return;
        }

        if (session.PlayerEntity.HasRaidStarted)
        {
            return;
        }

        if (session.PlayerEntity.IsInGroup())
        {
            session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.RAID_SHOUTMESSAGE_CANT_CREATE_RAID_GROUP, session.UserLanguage), MsgMessageType.Middle);
            return;
        }

        if (session.PlayerEntity.HasShopOpened)
        {
            return;
        }

        if (session.PlayerEntity.IsOnVehicle)
        {
            session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.ITEM_MESSAGE_CANT_USE, session.UserLanguage), ChatMessageColorType.Yellow);
            return;
        }

        if (!session.PlayerEntity.MapInstance.HasMapFlag(MapFlags.IS_BASE_MAP))
        {
            session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_MUST_BE_IN_CLASSIC_MAP, session.UserLanguage), MsgMessageType.Middle);
            return;
        }

        if (session.CantPerformActionOnAct4())
        {
            return;
        }

        if (!Enum.TryParse(e.RaidType.ToString(), out RaidType raidType))
        {
            return;
        }

        SRaidRequirement requirement = _raidScriptManager.GetScriptedRaid(raidType.ToSRaidType())?.Requirement;
        if (requirement == null)
        {
            session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.RAID_INFO_NO_EXIST, session.UserLanguage));
            return;
        }

        if (session.PlayerEntity.Level < requirement.MinimumLevel)
        {
            session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.RAID_CHATMESSAGE_LOW_LEVEL, session.UserLanguage, requirement.MinimumLevel.ToString()), ChatMessageColorType.Red);
            return;
        }

        if (session.PlayerEntity.HeroLevel < requirement.MinimumHeroLevel)
        {
            session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.RAID_CHATMESSAGE_LOW_LEVEL, session.UserLanguage, requirement.MinimumHeroLevel.ToString()), ChatMessageColorType.Red);
            return;
        }

        if (session.IsRaidTypeRestricted(raidType))
        {
            if (!session.IsPlayerWearingRaidAmulet(raidType))
            {
                string amuletName = raidType == RaidType.LordDraco
                    ? _itemsManager.GetItem((short)ItemVnums.DRACO_AMULET)?.GetItemName(_gameLanguage, session.UserLanguage)
                    : _itemsManager.GetItem((short)ItemVnums.GLACERUS_AMULET)?.GetItemName(_gameLanguage, session.UserLanguage);

                session.SendChatMessage(session.GetLanguageFormat(GameDialogKey.RAID_CHATMESSAGE_AMULET_NEEDED, amuletName), ChatMessageColorType.Yellow);
                return;
            }

            string getRaidName = session.GenerateRaidName(_gameLanguage, raidType);
            if (!session.CanPlayerJoinToRestrictedRaid(raidType))
            {
                session.SendChatMessage(session.GetLanguageFormat(GameDialogKey.RAID_CHATMESSAGE_LIMIT_REACHED, getRaidName), ChatMessageColorType.Yellow);
                return;
            }

            switch (raidType)
            {
                case RaidType.LordDraco:
                    session.SendChatMessage(session.GetLanguageFormat(GameDialogKey.RAID_CHATMESSAGE_LIMIT_LEFT,
                        getRaidName, session.PlayerEntity.RaidRestrictionDto.LordDraco), ChatMessageColorType.Yellow);
                    break;
                case RaidType.Glacerus:
                    session.SendChatMessage(session.GetLanguageFormat(GameDialogKey.RAID_CHATMESSAGE_LIMIT_LEFT,
                        getRaidName, session.PlayerEntity.RaidRestrictionDto.Glacerus), ChatMessageColorType.Yellow);
                    break;
            }
        }

        InventoryItem itemToRemove = e.ItemToRemove;
        if (itemToRemove != null)
        {
            await session.RemoveItemFromInventory(item: itemToRemove);
        }

        var raidInstance = new RaidParty(Guid.NewGuid(), raidType, requirement.MinimumLevel, requirement.MaximumLevel, requirement.MinimumHeroLevel, requirement.MaximumHeroLevel,
            requirement.MinimumParticipant,
            requirement.MaximumParticipant);

        raidInstance.AddMember(session);
        _raidManager.AddRaid(raidInstance);

        session.PlayerEntity.SetRaidParty(raidInstance);
        session.SendRaidPacket(RaidPacketType.LEADER_RELATED);
        session.SendRaidPacket(RaidPacketType.LIST_MEMBERS);
        session.SendRaidPacket(RaidPacketType.LEAVE);
        session.RefreshRaidMemberList();

        await session.EmitEventAsync(new RaidCreatedEvent());
    }
}