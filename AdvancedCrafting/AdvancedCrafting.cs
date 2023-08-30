using HarmonyLib;
using Shockah.Kokoro;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.AdvancedCrafting;

public class AdvancedCrafting : BaseMod<ModConfig>, ICraftingStatsApi
{
	private const string AssetPath = "Data/CraftingRecipeStats";
	private static int[] ItemQualities { get; set; } = { SObject.lowQuality, SObject.medQuality, SObject.highQuality, SObject.bestQuality };

	private static readonly Lazy<Func<CraftingPage, IList<Item>>> GetContainerContents = new(() => AccessTools.DeclaredMethod(typeof(CraftingPage), "getContainerContents").CreateDelegate<Func<CraftingPage, IList<Item>>>());
	private static readonly Lazy<Func<CraftingPage, int>> CurrentCraftingPageGetter = new(() => AccessTools.DeclaredField(typeof(CraftingPage), "currentCraftingPage").EmitInstanceGetter<CraftingPage, int>());

	private static int? CraftingQualityOverride = null;

	internal static AdvancedCrafting Instance { get; private set; } = null!;
	private ICraftingRecipeQualityInfoProvider CraftingRecipeQualityInfoProvider { get; set; } = null!;

	public override void OnEntry(IModHelper helper)
	{
		Instance = this;
		SetupCraftingRecipeQualityInfoProvider();
		helper.Events.Content.AssetRequested += OnAssetRequested;
		helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;

		Harmony harmony = new(ModManifest.UniqueID);
		harmony.TryPatch(
			monitor: Monitor,
			original: () => AccessTools.DeclaredMethod(typeof(CraftingPage), nameof(CraftingPage.receiveLeftClick)),
			prefix: new HarmonyMethod(GetType(), nameof(CraftingPage_receiveLeftClick_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(CraftingPage_receiveLeftClick_Finalizer))
		);
		harmony.TryPatch(
			monitor: Monitor,
			original: () => AccessTools.DeclaredMethod(typeof(CraftingPage), nameof(CraftingPage.receiveRightClick)),
			prefix: new HarmonyMethod(GetType(), nameof(CraftingPage_receiveRightClick_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(CraftingPage_receiveRightClick_Finalizer))
		);
		harmony.TryPatch(
			monitor: Monitor,
			original: () => AccessTools.DeclaredMethod(typeof(CraftingPage), "clickCraftingRecipe"),
			prefix: new HarmonyMethod(GetType(), nameof(CraftingPage_clickCraftingRecipe_Prefix))
		);
		harmony.TryPatch(
			monitor: Monitor,
			original: () => AccessTools.DeclaredMethod(typeof(CraftingRecipe), nameof(CraftingRecipe.ItemMatchesForCrafting)),
			postfix: new HarmonyMethod(GetType(), nameof(CraftingRecipe_ItemMatchesForCrafting_Postfix))
		);
		harmony.TryPatchVirtual(
			monitor: Monitor,
			original: () => AccessTools.DeclaredMethod(typeof(CraftingRecipe), nameof(CraftingRecipe.createItem)),
			postfix: new HarmonyMethod(GetType(), nameof(CraftingRecipe_createItem_Postfix))
		);
	}

	private void OnAssetRequested(object? args, AssetRequestedEventArgs e)
	{
		if (!e.Name.IsEquivalentTo(AssetPath))
			return;
		e.LoadFrom(() => new Dictionary<string, CraftingRecipeStats>(), AssetLoadPriority.Exclusive);
	}

	private void OnAssetsInvalidated(object? args, AssetsInvalidatedEventArgs e)
	{
		if (e.Names.Any(name => name.IsEquivalentTo("Data\\CraftingRecipes")))
			SetupCraftingRecipeQualityInfoProvider();
	}

	private void SetupCraftingRecipeQualityInfoProvider()
		=> CraftingRecipeQualityInfoProvider = new CompoundCraftingRecipeQualityInfoProvider(
			new RecipeIngredientCrafingRecipeQualityInfoProvider(),
			new EdibleItemCraftingRecipeQualityInfoProvider(),
			new CachingCraftingRecipeQualityInfoProvider()
		);

	private static void SetupCraftingQualityOverrideIfNeeded(CraftingPage page, CraftingRecipe recipe)
	{
		if (CraftingQualityOverride is not null)
			return;
		if (!Instance.RecipeSupportsQualityCrafting(recipe))
			return;

		foreach (var quality in ItemQualities.Reverse())
		{
			CraftingQualityOverride = quality;
			if (recipe.doesFarmerHaveIngredientsInInventory(GetContainerContents.Value(page)))
				return;
		}
	}

	private static void CraftingPage_receiveLeftClick_Prefix()
		=> CraftingQualityOverride = null;

	private static void CraftingPage_receiveLeftClick_Finalizer()
		=> CraftingQualityOverride = null;

	private static void CraftingPage_receiveRightClick_Prefix()
		=> CraftingQualityOverride = null;

	private static void CraftingPage_receiveRightClick_Finalizer()
		=> CraftingQualityOverride = null;

	private static void CraftingPage_clickCraftingRecipe_Prefix(CraftingPage __instance, ClickableTextureComponent c)
	{
		if (!__instance.pagesOfCraftingRecipes[CurrentCraftingPageGetter.Value(__instance)].TryGetValue(c, out var recipe))
			return;
		SetupCraftingQualityOverrideIfNeeded(__instance, recipe);
	}

	private static void CraftingRecipe_ItemMatchesForCrafting_Postfix(Item item, ref bool __result)
	{
		if (!__result)
			return;

		int minItemQuality = CraftingQualityOverride ?? ItemQualities[0];
		if (item.Quality < minItemQuality)
			__result = false;
	}

	private static void CraftingRecipe_createItem_Postfix(ref Item __result)
		=> __result.Quality = CraftingQualityOverride ?? ItemQualities[0];

	public bool RecipeSupportsQualityCrafting(CraftingRecipe recipe)
		=> CraftingRecipeQualityInfoProvider.RecipeSupportsQualityCrafting(recipe, () => false);

	public CraftingRecipeStats? GetRecipeStats(CraftingRecipe recipe)
		=> Game1.content.Load<Dictionary<string, CraftingRecipeStats>>(AssetPath).TryGetValue(recipe.name, out var stats) ? stats : null;

	public int? GetRecipeSkill(CraftingRecipe recipe)
		=> GetRecipeStats(recipe)?.Skill;

	public int? GetRecipeInspiration(CraftingRecipe recipe)
		=> GetRecipeStats(recipe)?.Inspiration;

	public int? GetRecipeMulticraft(CraftingRecipe recipe)
		=> GetRecipeStats(recipe)?.Multicraft;

	public int? GetRecipeResourcefulness(CraftingRecipe recipe)
		=> GetRecipeStats(recipe)?.Resourcefulness;
}