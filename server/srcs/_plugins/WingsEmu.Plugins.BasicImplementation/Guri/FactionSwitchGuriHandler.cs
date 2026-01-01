using System.Threading.Tasks;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.Game._Guri;
using WingsEmu.Game._Guri.Event;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Families.Event;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.Guri;

public class FactionSwitchGuriHandler : IGuriHandler
{
    private const int IndividualAngelEgg = 1;
    private const int IndividualDemonEgg = 2;
    private const int FamilyAngelEgg = 3;
    private const int FamilyDemonEgg = 4;
    public long GuriEffectId => 750;

    public async Task ExecuteAsync(IClientSession session, GuriEvent e)
    {
        int eggType = e.Data;
        int vnum = 1623 + eggType;
        var targetFaction = (FactionType)eggType;

        bool hasItem = session.PlayerEntity.HasItem(vnum);

        if (!hasItem)
        {
            return;
        }

        if (session.PlayerEntity.IsOnVehicle)
        {
            return;
        }

        if (session.CantPerformActionOnAct4())
        {
            return;
        }

        switch (eggType)
        {
            case IndividualAngelEgg:
            case IndividualDemonEgg:
            {
                if (session.PlayerEntity.IsInFamily())
                {
                    return;
                }

                if (session.PlayerEntity.Faction == targetFaction)
                {
                    return;
                }

                await session.EmitEventAsync(new ChangeFactionEvent
                {
                    NewFaction = targetFaction
                });

                await session.RemoveItemFromInventory(vnum);
                break;
            }
            case FamilyAngelEgg:
            case FamilyDemonEgg:
            {
                await session.EmitEventAsync(new FamilyChangeFactionEvent
                {
                    Faction = eggType / 2
                });
                break;
            }
        }
    }
}