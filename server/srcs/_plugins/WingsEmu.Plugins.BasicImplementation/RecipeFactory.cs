using System.Collections.Generic;
using WingsEmu.DTOs.Recipes;
using WingsEmu.Game;

namespace WingsEmu.Plugins.BasicImplementations;

public class RecipeFactory : IRecipeFactory
{
    private readonly List<RecipeItemDTO> _emptyItemDtos = new();

    public Recipe CreateRecipe(RecipeDTO recipeDto) => new Recipe(recipeDto.Id, recipeDto.Amount, recipeDto.ProducerMapNpcId, recipeDto.ProducerItemVnum, recipeDto.ProducerNpcVnum,
        recipeDto.ProducedItemVnum, recipeDto.Items ?? _emptyItemDtos);
}