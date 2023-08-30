using Shockah.Kokoro;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.AdvancedCrafting;

internal static class CraftingRecipeExt
{
	public static IEnumerable<Item> GetItemsToProduce(this CraftingRecipe recipe)
		=> recipe.itemToProduce
			.Select(id => ItemRegistry.Create(recipe.bigCraftable ? ("(BC)" + id) : ("(O)" + id), allowNull: true))
			.WhereNotNull();
}
