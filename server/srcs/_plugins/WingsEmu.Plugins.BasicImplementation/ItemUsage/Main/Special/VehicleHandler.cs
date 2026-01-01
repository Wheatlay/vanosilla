using System;
using System.Threading.Tasks;
using WingsAPI.Game.Extensions.Groups;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage;
using WingsEmu.Game._ItemUsage.Configuration;
using WingsEmu.Game._ItemUsage.Event;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Extensions.Mates;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Mates.Events;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Networking.Broadcasting;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Character;
using WingsEmu.Packets.Enums.Chat;
using WingsEmu.Plugins.BasicImplementations.Vehicles;

namespace WingsEmu.Plugins.BasicImplementations.ItemUsage.Main.Special;

public class VehicleHandler : IItemHandler
{
    private readonly IDelayManager _delayManager;
    private readonly IGameLanguageService _languageService;
    private readonly IMapManager _mapManager;
    private readonly IVehicleConfigurationProvider _provider;
    private readonly ISpPartnerConfiguration _spPartner;

    public VehicleHandler(IDelayManager delayManager, IGameLanguageService languageService, IVehicleConfigurationProvider provider, ISpPartnerConfiguration spPartner, IMapManager mapManager)
    {
        _delayManager = delayManager;
        _languageService = languageService;
        _provider = provider;
        _spPartner = spPartner;
        _mapManager = mapManager;
    }

    public ItemType ItemType => ItemType.Special;
    public long[] Effects => new long[] { 1000 };

    public async Task HandleAsync(IClientSession session, InventoryUseItemEvent e)
    {
        if (!session.HasCurrentMapInstance)
        {
            // log it and report
            return;
        }

        if (session.PlayerEntity.RainbowBattleComponent.IsInRainbowBattle)
        {
            session.SendChatMessage(session.GetLanguage(GameDialogKey.ITEM_MESSAGE_CANT_USE), ChatMessageColorType.Yellow);
            return;
        }

        if (session.PlayerEntity.HasShopOpened)
        {
            // log it and report for abusing
            return;
        }

        if (session.PlayerEntity.IsInExchange())
        {
            return;
        }

        if (!session.PlayerEntity.IsAlive())
        {
            return;
        }

        if (session.PlayerEntity.IsCastingSkill)
        {
            return;
        }

        if (session.PlayerEntity.LastSkillUse.AddSeconds(2) > DateTime.UtcNow)
        {
            return;
        }

        if (!session.PlayerEntity.IsOnVehicle && session.PlayerEntity.BuffComponent.HasBuff(BuffGroup.Bad))
        {
            session.SendMsg(_languageService.GetLanguage(GameDialogKey.ITEM_SHOUTMESSAGE_VEHICLE_DEBUFF, session.UserLanguage), MsgMessageType.Middle);
            return;
        }

        VehicleConfiguration vehicle = _provider.GetByVehicleVnum(e.Item.ItemInstance.ItemVNum);

        if (vehicle == null)
        {
            return;
        }

        if (session.PlayerEntity.IsMorphed)
        {
            session.SendChatMessage(_languageService.GetLanguage(GameDialogKey.ITEM_MESSAGE_CANT_USE, session.UserLanguage), ChatMessageColorType.Yellow);
            return;
        }

        if (e.Option == 0 && !session.PlayerEntity.IsOnVehicle)
        {
            if (session.PlayerEntity.IsSitting)
            {
                await session.RestAsync();
            }

            DateTime waitUntil = await _delayManager.RegisterAction(session.PlayerEntity, DelayedActionType.EquipVehicle);
            session.SendDelay((int)(waitUntil - DateTime.UtcNow).TotalMilliseconds, GuriType.Transforming,
                $"u_i 1 {session.PlayerEntity.Id} {(byte)e.Item.ItemInstance.GameItem.Type} {e.Item.Slot} 2");
            return;
        }

        if (!session.PlayerEntity.IsOnVehicle && e.Option != 0)
        {
            bool canEquipVehicle = await _delayManager.CanPerformAction(session.PlayerEntity, DelayedActionType.EquipVehicle);
            if (!canEquipVehicle)
            {
                return;
            }

            await _delayManager.CompleteAction(session.PlayerEntity, DelayedActionType.EquipVehicle);

            session.PlayerEntity.IsOnVehicle = true;
            session.PlayerEntity.VehicleSpeed = (byte)vehicle.DefaultSpeed;
            session.PlayerEntity.MorphUpgrade = 0;
            session.PlayerEntity.MorphUpgrade2 = 0;
            session.PlayerEntity.Morph = session.PlayerEntity.Gender == GenderType.Male ? vehicle.MaleMorphId : vehicle.FemaleMorphId;

            foreach (IMateEntity x in session.PlayerEntity.MateComponent.TeamMembers())
            {
                if (x.IsSitting && x.IsAlive())
                {
                    await session.EmitEventAsync(new MateRestEvent
                    {
                        MateEntity = x,
                        Force = true
                    });
                }

                session.Broadcast(x.GenerateOut());
            }

            session.RefreshParty(_spPartner);

            session.BroadcastEffect((int)EffectType.Transform, new RangeBroadcast(session.PlayerEntity.PositionX, session.PlayerEntity.PositionY));
            session.PlayerEntity.RefreshCharacterStats();
            session.SendCondPacket();
            session.BroadcastCMode();

            session.PlayerEntity.LastSpeedChange = DateTime.UtcNow;

            await session.EmitEventAsync(new VehicleCheckMapSpeedEvent());

            if (vehicle.RemoveItem is true)
            {
                await session.RemoveItemFromInventory(item: e.Item);
            }
        }
        else if (session.PlayerEntity.IsOnVehicle)
        {
            await session.EmitEventAsync(new RemoveVehicleEvent(true));
        }
    }
}