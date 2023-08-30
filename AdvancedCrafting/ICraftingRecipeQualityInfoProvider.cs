using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.AdvancedCrafting;

internal interface ICraftingRecipeQualityInfoProvider
{
	bool RecipeSupportsQualityCrafting(CraftingRecipe recipe, Func<bool> result);
}

internal sealed class EdibleItemCraftingRecipeQualityInfoProvider : ICraftingRecipeQualityInfoProvider
{
	public bool RecipeSupportsQualityCrafting(CraftingRecipe recipe, Func<bool> result)
		=> recipe.GetItemsToProduce().OfType<SObject>().All(item => item.Edibility != SObject.inedible) || result();
}

internal sealed class RecipeIngredientCrafingRecipeQualityInfoProvider : ICraftingRecipeQualityInfoProvider
{
	private readonly HashSet<CraftingRecipe> CurrentlyChecking = new();

	public bool RecipeSupportsQualityCrafting(CraftingRecipe recipe, Func<bool> result)
	{
		if (!CurrentlyChecking.Add(recipe))
			return result();

		var itemsToProduce = recipe.GetItemsToProduce()
			.OfType<SObject>()
			.ToList();
		if (itemsToProduce.Count == 0)
		{
			CurrentlyChecking.Remove(recipe);
			return result();
		}

		var newResult = CraftingRecipe.craftingRecipes.Keys
			.Where(name => recipe.name != name)
			.Select(name => new CraftingRecipe(name))
			.Where(AdvancedCrafting.Instance.RecipeSupportsQualityCrafting)
			.Any(recipe => recipe.recipeList.Keys.Any(id => itemsToProduce.Any(item => CraftingRecipe.ItemMatchesForCrafting(item, id))));
		CurrentlyChecking.Remove(recipe);
		return newResult || result();
	}
}

internal sealed class CachingCraftingRecipeQualityInfoProvider : ICraftingRecipeQualityInfoProvider
{
	private readonly Dictionary<string, bool> Cache = new();

	public bool RecipeSupportsQualityCrafting(CraftingRecipe recipe, Func<bool> result)
	{
		if (Cache.TryGetValue(recipe.name, out var cachedResult))
			return cachedResult;

		var newResult = result();
		Cache[recipe.name] = newResult;
		return newResult;
	}
}

internal sealed class CompoundCraftingRecipeQualityInfoProvider : ICraftingRecipeQualityInfoProvider
{
	private readonly ICraftingRecipeQualityInfoProvider[] Providers;

	public CompoundCraftingRecipeQualityInfoProvider(params ICraftingRecipeQualityInfoProvider[] providers) : this((IEnumerable<ICraftingRecipeQualityInfoProvider>)providers) { }

	public CompoundCraftingRecipeQualityInfoProvider(IEnumerable<ICraftingRecipeQualityInfoProvider> providers)
	{
		this.Providers = providers.ToArray();
	}

	public bool RecipeSupportsQualityCrafting(CraftingRecipe recipe, Func<bool> result)
	{
		if (Providers.Length == 0)
			return result();
		return MakeResultFunction(recipe, result, Providers.Length - 1)();
	}

	private Func<bool> MakeResultFunction(CraftingRecipe recipe, Func<bool> originalResult, int i)
		=> i < 0 ? originalResult : () => Providers[i].RecipeSupportsQualityCrafting(recipe, MakeResultFunction(recipe, originalResult, i - 1));
}