using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Tools;
using StardewValley.Locations;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes;

partial class ModConfig
{
	[JsonProperty] public float InflationIncrease { get; internal set; } = 0.2f;
}

internal sealed class InflationAffix : BaseSeasonAffix, ISeasonAffix
{
	private static bool IsHarmonySetup = false;

	private static string ShortID => "Inflation";
	public string LocalizedDescription => Mod.Helper.Translation.Get($"{I18nPrefix}.description", new { Increase = $"{(int)(Mod.Config.InflationIncrease * 100):0.##}%" });
	public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(272, 528, 16, 16));

	private readonly PerScreen<List<WeakReference<object>>> HandledContexts = new(() => new());
	private readonly PerScreen<List<WeakReference<ShopMenu>>> HandledShopMenus = new(() => new());

	public InflationAffix() : base(ShortID, "neutral") { }

	public int GetPositivity(OrdinalSeason season)
		=> 1;

	public int GetNegativity(OrdinalSeason season)
		=> 1;

	public void OnRegister()
		=> Apply(Mod.Harmony);

	public void OnActivate(AffixActivationContext context)
	{
		Mod.Helper.Events.Content.AssetRequested += OnAssetRequested;
		Mod.Helper.Events.GameLoop.DayStarted += OnDayStarted;
		Mod.Helper.Events.Display.MenuChanged += OnMenuChanged;
		Mod.Helper.GameContent.InvalidateCache("Strings\\Locations");
	}

	public void OnDeactivate(AffixActivationContext context)
	{
		Mod.Helper.Events.Content.AssetRequested -= OnAssetRequested;
		Mod.Helper.Events.GameLoop.DayStarted -= OnDayStarted;
		Mod.Helper.Events.Display.MenuChanged -= OnMenuChanged;
		Mod.Helper.GameContent.InvalidateCache("Strings\\Locations");
		PruneHandledContexts();
	}

	public void SetupConfig(IManifest manifest)
	{
		var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
		GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
		helper.AddNumberOption($"{I18nPrefix}.config.increase", () => Mod.Config.InflationIncrease, min: 0.05f, max: 4f, interval: 0.05f, value => $"{(int)(value * 100):0.##}%");
	}

	private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
	{
		if (e.Name.IsEquivalentTo("Strings\\Locations"))
		{
			e.Edit(rawAsset =>
			{
				var asset = rawAsset.AsDictionary<string, string>();
				asset.Data["BusStop_BuyTicketToDesert"] = asset.Data["BusStop_BuyTicketToDesert"].Replace("500", $"{GetModifiedPrice(500)}");
				asset.Data["ScienceHouse_Carpenter_UpgradeHouse1"] = asset.Data["ScienceHouse_Carpenter_UpgradeHouse1"].Replace("10,000", $"{GetModifiedPrice(10000):#,##0}").Replace("10.000", $"{GetModifiedPrice(10000):#.##0}");
				asset.Data["ScienceHouse_Carpenter_UpgradeHouse2"] = asset.Data["ScienceHouse_Carpenter_UpgradeHouse2"].Replace("50,000", $"{GetModifiedPrice(50000):#,##0}").Replace("50.000", $"{GetModifiedPrice(50000):#.##0}");
				asset.Data["ScienceHouse_Carpenter_UpgradeHouse3"] = asset.Data["ScienceHouse_Carpenter_UpgradeHouse3"].Replace("100,000", $"{GetModifiedPrice(100000):#,##0}").Replace("100.000", $"{GetModifiedPrice(100000):#.##0}");
				asset.Data["ScienceHouse_Carpenter_CommunityUpgrade1"] = asset.Data["ScienceHouse_Carpenter_CommunityUpgrade1"].Replace("500,000", $"{GetModifiedPrice(500000):#,##0}").Replace("500.000", $"{GetModifiedPrice(500000):#.##0}");
				asset.Data["ScienceHouse_Carpenter_CommunityUpgrade2"] = asset.Data["ScienceHouse_Carpenter_CommunityUpgrade2"].Replace("300,000", $"{GetModifiedPrice(300000):#,##0}").Replace("300.000", $"{GetModifiedPrice(300000):#.##0}");
			});
		}
		else if (e.Name.IsEquivalentTo("Data\\Buildings"))
		{
			e.Edit(rawAsset =>
			{
				var asset = rawAsset.AsDictionary<string, BuildingData>();
				foreach (var (key, building) in asset.Data)
					building.BuildCost = GetModifiedPrice(building.BuildCost);
			});
		}
		else if (e.Name.IsEquivalentTo("Data\\Tools"))
		{
			e.Edit(rawAsset =>
			{
				var asset = rawAsset.AsDictionary<string, ToolData>();
				foreach (var (key, tool) in asset.Data)
					if (tool.SalePrice > 0)
						tool.SalePrice = GetModifiedPrice(tool.SalePrice);
			});
		}
	}

	private void OnDayStarted(object? sender, DayStartedEventArgs e)
		=> PruneHandledContexts();

	[EventPriority(EventPriority.Low - 10)]
	private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
	{
		if (e.NewMenu is not ShopMenu menu)
			return;
		if (menu.currency != 0)
			return;
		if (HandledShopMenus.Value.Any(r => r.TryGetTarget(out var handledMenu) && ReferenceEquals(handledMenu, menu)))
			return;

		foreach (var kvp in menu.itemPriceAndStock.ToList())
			menu.itemPriceAndStock[kvp.Key] = new(
				price: GetModifiedPrice(kvp.Value.Price, kvp.Key),
				stock: kvp.Value.Stock,
				tradeItem: kvp.Value.TradeItem,
				tradeItemCount: kvp.Value.TradeItemCount,
				stockMode: kvp.Value.LimitedStockMode,
				syncedKey: kvp.Value.SyncedKey,
				itemToSyncStack: kvp.Value.ItemToSyncStack,
				stackDrawType: kvp.Value.StackDrawType
			);
	}

	private void Apply(Harmony harmony)
	{
		if (IsHarmonySetup)
			return;
		IsHarmonySetup = true;

		harmony.TryPatchVirtual(
			monitor: Mod.Monitor,
			original: () => AccessTools.Method(typeof(SObject), nameof(SObject.sellToStorePrice)),
			postfix: new HarmonyMethod(GetType(), nameof(SObject_sellToStorePrice_Postfix))
		);
		harmony.TryPatch(
			monitor: Mod.Monitor,
			original: () => AccessTools.DeclaredConstructor(typeof(BusStop), new Type[] { typeof(string), typeof(string) }),
			postfix: new HarmonyMethod(GetType(), nameof(BusStop_ctor_Postfix))
		);
		harmony.TryPatch(
			monitor: Mod.Monitor,
			original: () => AccessTools.DeclaredConstructor(typeof(BoatTunnel), new Type[] { typeof(string), typeof(string) }),
			postfix: new HarmonyMethod(GetType(), nameof(BoatTunnel_ctor_Postfix))
		);
		harmony.TryPatch(
			monitor: Mod.Monitor,
			original: () => AccessTools.Method(typeof(GameLocation), "houseUpgradeAccept"),
			transpiler: new HarmonyMethod(GetType(), nameof(GameLocation_houseUpgradeAccept_Transpiler))
		);
		harmony.TryPatch(
			monitor: Mod.Monitor,
			original: () => AccessTools.Method(typeof(GameLocation), "communityUpgradeAccept"),
			transpiler: new HarmonyMethod(GetType(), nameof(GameLocation_communityUpgradeAccept_Transpiler))
		);

		// TODO: reimplement Json Assets integration, if needed
		//if (Mod.Helper.ModRegistry.IsLoaded("spacechase0.JsonAssets"))
		//{
		//	harmony.TryPatch(
		//		monitor: Mod.Monitor,
		//		original: () => AccessTools.Method(AccessTools.TypeByName("JsonAssets.Mod, JsonAssets"), "OnMenuChanged"),
		//		transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(JsonAssets_Mod_OnMenuChanged_Transpiler)))
		//	);
		//}

		// TODO: reimplement Dynamic Game Assets integration, if needed
		//if (Mod.Helper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets"))
		//{
		//	harmony.TryPatch(
		//		monitor: Mod.Monitor,
		//		original: () => AccessTools.Method(AccessTools.TypeByName("DynamicGameAssets.ShopEntry, DynamicGameAssets"), "AddToShop"),
		//		transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(DynamicGameAssets_ShopEntry_AddToShopOrAddToShopStock_Transpiler)))
		//	);
		//	harmony.TryPatch(
		//		monitor: Mod.Monitor,
		//		original: () => AccessTools.Method(AccessTools.TypeByName("DynamicGameAssets.ShopEntry, DynamicGameAssets"), "AddToShopStock"),
		//		transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(DynamicGameAssets_ShopEntry_AddToShopOrAddToShopStock_Transpiler)))
		//	);
		//}
	}

	private void PruneHandledContexts()
		=> HandledContexts.Value.RemoveAll(r => !r.TryGetTarget(out _));

	public static int GetModifiedPrice(int originalPrice)
		=> (int)Math.Round(originalPrice * (1f + Mod.Config.InflationIncrease));

	private int GetModifiedPrice(int originalPrice, object? context)
	{
		if (context is null)
			return GetModifiedPrice(originalPrice);
		if (HandledContexts.Value.Any(r => r.TryGetTarget(out var handledContext) && ReferenceEquals(handledContext, context)))
			return originalPrice;
		HandledContexts.Value.Add(new(context));
		return GetModifiedPrice(originalPrice);
	}

	private static void SObject_sellToStorePrice_Postfix(ref int __result)
	{
		if (__result <= 0)
			return;
		var affix = Mod.ActiveAffixes.OfType<InflationAffix>().FirstOrDefault();
		if (affix is null)
			return;
		__result = GetModifiedPrice(__result);
	}

	private static void BusStop_ctor_Postfix(BusStop __instance)
		=> __instance.TicketPrice = GetModifiedPrice(__instance.TicketPrice);

	private static void BoatTunnel_ctor_Postfix(BoatTunnel __instance)
		=> __instance.TicketPrice = GetModifiedPrice(__instance.TicketPrice);

	private static IEnumerable<CodeInstruction> GameLocation_houseUpgradeAccept_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.ForEach(
					SequenceMatcherRelativeBounds.WholeSequence,
					[
						ILMatches.LdcI4(10000)
					],
					matcher =>
					{
						return matcher
							.Insert(
								SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
								new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice), new Type[] { typeof(int) }))
							);
					},
					minExpectedOccurences: 3,
					maxExpectedOccurences: 3
				)
				.ForEach(
					SequenceMatcherRelativeBounds.WholeSequence,
					[
						ILMatches.LdcI4(50000)
					],
					matcher =>
					{
						return matcher
							.Insert(
								SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
								new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice), new Type[] { typeof(int) }))
							);
					},
					minExpectedOccurences: 3,
					maxExpectedOccurences: 3
				)
				.ForEach(
					SequenceMatcherRelativeBounds.WholeSequence,
					[
						ILMatches.LdcI4(100000)
					],
					matcher =>
					{
						return matcher
							.Insert(
								SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
								new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice), new Type[] { typeof(int) }))
							);
					},
					minExpectedOccurences: 3,
					maxExpectedOccurences: 3
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Mod.Monitor.Log($"Could not patch method {originalMethod} - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
			return instructions;
		}
	}

	private static IEnumerable<CodeInstruction> GameLocation_communityUpgradeAccept_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.ForEach(
					SequenceMatcherRelativeBounds.WholeSequence,
					[
						ILMatches.LdcI4(500000)
					],
					matcher =>
					{
						return matcher
							.Insert(
								SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
								new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice), new Type[] { typeof(int) }))
							);
					},
					minExpectedOccurences: 3,
					maxExpectedOccurences: 3
				)
				.ForEach(
					SequenceMatcherRelativeBounds.WholeSequence,
					[
						ILMatches.LdcI4(300000)
					],
					matcher =>
					{
						return matcher
							.Insert(
								SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
								new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice), new Type[] { typeof(int) }))
							);
					},
					minExpectedOccurences: 3,
					maxExpectedOccurences: 3
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Mod.Monitor.Log($"Could not patch method {originalMethod} - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
			return instructions;
		}
	}

	//private static IEnumerable<CodeInstruction> JsonAssets_Mod_OnMenuChanged_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	//{
	//	try
	//	{
	//		return new SequenceBlockMatcher<CodeInstruction>(instructions)
	//			.Find(ILMatches.Instruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<ISalable, int[]>), nameof(Dictionary<ISalable, int[]>.Add), new Type[] { typeof(ISalable), typeof(int[]) })))
	//			.Replace(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(JsonAssetsOrDynamicGameAssets_Mod_OnMenuChanged_Transpiler_ModifyValues))))
	//			.AllElements();
	//	}
	//	catch (Exception ex)
	//	{
	//		Mod.Monitor.Log($"Could not patch method {originalMethod} - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
	//		return instructions;
	//	}
	//}

	//private static IEnumerable<CodeInstruction> DynamicGameAssets_ShopEntry_AddToShopOrAddToShopStock_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	//{
	//	try
	//	{
	//		return new SequenceBlockMatcher<CodeInstruction>(instructions)
	//			.ForEach(
	//				SequenceMatcherRelativeBounds.WholeSequence,
	//				new IElementMatch<CodeInstruction>[]
	//				{
	//					ILMatches.Instruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<ISalable, int[]>), nameof(Dictionary<ISalable, int[]>.Add), new Type[] { typeof(ISalable), typeof(int[]) }))
	//				},
	//				matcher =>
	//				{
	//					return matcher
	//						.Replace(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(JsonAssetsOrDynamicGameAssets_Mod_OnMenuChanged_Transpiler_ModifyValues))));
	//				},
	//				minExpectedOccurences: 3,
	//				maxExpectedOccurences: 3
	//			)
	//			.AllElements();
	//	}
	//	catch (Exception ex)
	//	{
	//		Mod.Monitor.Log($"Could not patch method {originalMethod} - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
	//		return instructions;
	//	}
	//}

	//public static void JsonAssetsOrDynamicGameAssets_Mod_OnMenuChanged_Transpiler_ModifyValues(Dictionary<ISalable, int[]> stock, ISalable item, int[] values)
	//{
	//	if (values.Length != 2)
	//	{
	//		stock.Add(item, values);
	//		return;
	//	}

	//	var affix = Mod.ActiveAffixes.OfType<InflationAffix>().FirstOrDefault();
	//	if (affix is null)
	//	{
	//		stock.Add(item, values);
	//		return;
	//	}

	//	affix.ModifyPrice(ref values[0], item);
	//	stock.Add(item, values);
	//}
}