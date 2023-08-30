using StardewValley;

namespace Shockah.AdvancedCrafting;

public interface ICraftingStatsApi
{
	bool RecipeSupportsQualityCrafting(CraftingRecipe recipe);

	CraftingRecipeStats? GetRecipeStats(CraftingRecipe recipe);
	int? GetRecipeSkill(CraftingRecipe recipe);
	int? GetRecipeInspiration(CraftingRecipe recipe);
	int? GetRecipeMulticraft(CraftingRecipe recipe);
	int? GetRecipeResourcefulness(CraftingRecipe recipe);
}