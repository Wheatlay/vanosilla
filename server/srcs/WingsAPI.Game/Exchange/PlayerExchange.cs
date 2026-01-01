using System.Collections.Generic;
using WingsEmu.Game.Inventory;

namespace WingsEmu.Game.Exchange;

public class PlayerExchange
{
    public PlayerExchange(long senderId, long targetId)
    {
        SenderId = senderId;
        TargetId = targetId;
    }

    public long SenderId { get; }

    public long TargetId { get; }

    public List<(InventoryItem, short)> Items { get; set; }

    public int Gold { get; set; }

    public long BankGold { get; set; }

    public bool RegisteredItems { get; set; }

    public bool AcceptedTrade { get; set; }
}