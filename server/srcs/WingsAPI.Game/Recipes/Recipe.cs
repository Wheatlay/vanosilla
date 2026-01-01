// WingsEmu
// 
// Developed by NosWings Team

using System.Collections.Generic;
using WingsEmu.DTOs.Recipes;
using WingsEmu.Game._packetHandling;

namespace WingsEmu.Game;

public class Recipe
{
    public Recipe(int id, int amount, int? producerMapNpcId, int? producerItemVnum, int? producerNpcVnum, int producedItemVnum, IReadOnlyList<RecipeItemDTO> items)
    {
        Id = id;
        Amount = amount;
        ProducerMapNpcId = producerMapNpcId;
        ProducerItemVnum = producerItemVnum;
        ProducerNpcVnum = producerNpcVnum;
        ProducedItemVnum = producedItemVnum;
        Items = items;
    }

    public int Id { get; }
    public int Amount { get; }
    public int? ProducerMapNpcId { get; }
    public int? ProducerItemVnum { get; }
    public int? ProducerNpcVnum { get; }
    public int ProducedItemVnum { get; }
    public IReadOnlyList<RecipeItemDTO> Items { get; }
}

public class RecipeOpenWindowEvent : PlayerEvent
{
    public RecipeOpenWindowEvent(int itemVnum) => ItemVnum = itemVnum;

    public int ItemVnum { get; }
}