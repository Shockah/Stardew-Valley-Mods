using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Neutral
{
	internal sealed class InflationAffix : BaseSeasonAffix, ISeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Inflation";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.description", new { Increase = $"{(int)(Mod.Config.InflationIncrease * 100):0.##}%" });
		public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(272, 528, 16, 16));

		public InflationAffix() : base($"{Mod.ModManifest.UniqueID}.{ShortID}") { }

		public int GetPositivity(OrdinalSeason season)
			=> 1;

		public int GetNegativity(OrdinalSeason season)
			=> 1;

		public void OnRegister()
			=> Apply(Mod.Harmony);

		public void OnActivate()
		{
			Mod.Helper.Events.Content.AssetRequested += OnAssetRequested;
			Mod.Helper.GameContent.InvalidateCache("Strings\\Locations");
		}

		public void OnDeactivate()
		{
			Mod.Helper.Events.Content.AssetRequested -= OnAssetRequested;
			Mod.Helper.GameContent.InvalidateCache("Strings\\Locations");
		}

		public void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.neutral.{ShortID}.config.increase", () => Mod.Config.InflationIncrease, min: 0.05f, max: 4f, interval: 0.05f, value => $"{(int)(value * 100):0.##}%");
		}

		private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
		{
			if (!e.Name.IsEquivalentTo("Strings\\Locations"))
				return;
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

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatchVirtual(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(Item), nameof(Item.salePrice)),
				postfix: new HarmonyMethod(AccessTools.Method(GetType(), nameof(Item_salePrice_Postfix)))
			);
			harmony.TryPatchVirtual(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(SObject), nameof(SObject.sellToStorePrice)),
				postfix: new HarmonyMethod(AccessTools.Method(GetType(), nameof(SObject_sellToStorePrice_Postfix)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Constructor(typeof(ShopMenu), new Type[] { typeof(Dictionary<ISalable, int[]>), typeof(int), typeof(string), typeof(Func<ISalable, Farmer, int, bool>), typeof(Func<ISalable, bool>), typeof(string) }),
				postfix: new HarmonyMethod(AccessTools.Method(GetType(), nameof(ShopMenu_ctor_Postfix)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Constructor(typeof(BluePrint), new Type[] { typeof(string) }),
				postfix: new HarmonyMethod(AccessTools.Method(GetType(), nameof(BluePrint_ctor_Postfix)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(Utility), "priceForToolUpgradeLevel"),
				postfix: new HarmonyMethod(AccessTools.Method(GetType(), nameof(Utility_priceForToolUpgradeLevel_Postfix)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(BusStop), nameof(BusStop.answerDialogue)),
				transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(BusStop_answerDialogue_Transpiler)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(BoatTunnel), nameof(BoatTunnel.checkAction)),
				transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(BoatTunnel_checkAction_Transpiler)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(BoatTunnel), nameof(BoatTunnel.answerDialogue)),
				transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(BoatTunnel_answerDialogue_Transpiler)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(GameLocation), "houseUpgradeAccept"),
				transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(GameLocation_houseUpgradeAccept_Transpiler)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(GameLocation), "communityUpgradeAccept"),
				transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(GameLocation_communityUpgradeAccept_Transpiler)))
			);

			if (Mod.Helper.ModRegistry.IsLoaded("spacechase0.JsonAssets"))
			{
				harmony.TryPatch(
					monitor: Mod.Monitor,
					original: () => AccessTools.Method(AccessTools.TypeByName("JsonAssets.Mod, JsonAssets"), "OnMenuChanged"),
					transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(JsonAssets_Mod_OnMenuChanged_Transpiler)))
				);
			}

			if (Mod.Helper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets"))
			{
				harmony.TryPatch(
					monitor: Mod.Monitor,
					original: () => AccessTools.Method(AccessTools.TypeByName("DynamicGameAssets.ShopEntry, DynamicGameAssets"), "AddToShop"),
					transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(DynamicGameAssets_ShopEntry_AddToShopOrAddToShopStock_Transpiler)))
				);
				harmony.TryPatch(
					monitor: Mod.Monitor,
					original: () => AccessTools.Method(AccessTools.TypeByName("DynamicGameAssets.ShopEntry, DynamicGameAssets"), "AddToShopStock"),
					transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(DynamicGameAssets_ShopEntry_AddToShopOrAddToShopStock_Transpiler)))
				);
			}
		}

		public static int GetModifiedPrice(int originalPrice)
			=> (int)Math.Round(originalPrice * (1f + Mod.Config.InflationIncrease));

		private static void ModifyPrice(ref int price)
			=> price = GetModifiedPrice(price);

		private static void Item_salePrice_Postfix(ref int __result)
		{
			if (__result <= 0)
				return;
			if (!Mod.ActiveAffixes.Any(a => a is InflationAffix))
				return;
			ModifyPrice(ref __result);
		}

		private static void SObject_sellToStorePrice_Postfix(ref int __result)
		{
			if (__result <= 0)
				return;
			if (!Mod.ActiveAffixes.Any(a => a is InflationAffix))
				return;
			ModifyPrice(ref __result);
		}

		private static void ShopMenu_ctor_Postfix(ShopMenu __instance, int currency)
		{
			if (currency != 0)
				return;
			if (!Mod.ActiveAffixes.Any(a => a is InflationAffix))
				return;
			foreach (var kvp in __instance.itemPriceAndStock)
				if (kvp.Value.Length == 2)
					ModifyPrice(ref kvp.Value[0]);
		}

		private static void BluePrint_ctor_Postfix(BluePrint __instance)
		{
			if (!Mod.ActiveAffixes.Any(a => a is InflationAffix))
				return;
			ModifyPrice(ref __instance.moneyRequired);
		}

		private static void Utility_priceForToolUpgradeLevel_Postfix(ref int __result)
		{
			if (!Mod.ActiveAffixes.Any(a => a is InflationAffix))
				return;
			ModifyPrice(ref __result);
		}

		private static IEnumerable<CodeInstruction> BusStop_answerDialogue_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.ForEach(
						SequenceMatcherRelativeBounds.WholeSequence,
						new IElementMatch<CodeInstruction>[]
						{
							ILMatches.LdcI4(500)
						},
						matcher =>
						{
							return matcher
								.Insert(
									SequenceMatcherPastBoundsDirection.After, true,
									new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice)))
								);
						},
						minExpectedOccurences: 3,
						maxExpectedOccurences: 3
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Mod.Monitor.Log($"Could not patch methods - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		private static IEnumerable<CodeInstruction> BoatTunnel_checkAction_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.ForEach(
						SequenceMatcherRelativeBounds.WholeSequence,
						new IElementMatch<CodeInstruction>[]
						{
							ILMatches.Call("GetTicketPrice")
						},
						matcher =>
						{
							return matcher
								.Insert(
									SequenceMatcherPastBoundsDirection.After, true,
									new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice)))
								);
						},
						minExpectedOccurences: 2,
						maxExpectedOccurences: 2
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Mod.Monitor.Log($"Could not patch methods - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		private static IEnumerable<CodeInstruction> BoatTunnel_answerDialogue_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.Find(ILMatches.Call("GetTicketPrice"))
					.Insert(
						SequenceMatcherPastBoundsDirection.After, true,
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice)))
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Mod.Monitor.Log($"Could not patch methods - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		private static IEnumerable<CodeInstruction> GameLocation_houseUpgradeAccept_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.ForEach(
						SequenceMatcherRelativeBounds.WholeSequence,
						new IElementMatch<CodeInstruction>[]
						{
							ILMatches.LdcI4(10000)
						},
						matcher =>
						{
							return matcher
								.Insert(
									SequenceMatcherPastBoundsDirection.After, true,
									new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice)))
								);
						},
						minExpectedOccurences: 3,
						maxExpectedOccurences: 3
					)
					.ForEach(
						SequenceMatcherRelativeBounds.WholeSequence,
						new IElementMatch<CodeInstruction>[]
						{
							ILMatches.LdcI4(50000)
						},
						matcher =>
						{
							return matcher
								.Insert(
									SequenceMatcherPastBoundsDirection.After, true,
									new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice)))
								);
						},
						minExpectedOccurences: 3,
						maxExpectedOccurences: 3
					)
					.ForEach(
						SequenceMatcherRelativeBounds.WholeSequence,
						new IElementMatch<CodeInstruction>[]
						{
							ILMatches.LdcI4(100000)
						},
						matcher =>
						{
							return matcher
								.Insert(
									SequenceMatcherPastBoundsDirection.After, true,
									new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice)))
								);
						},
						minExpectedOccurences: 3,
						maxExpectedOccurences: 3
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Mod.Monitor.Log($"Could not patch methods - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		private static IEnumerable<CodeInstruction> GameLocation_communityUpgradeAccept_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.ForEach(
						SequenceMatcherRelativeBounds.WholeSequence,
						new IElementMatch<CodeInstruction>[]
						{
							ILMatches.LdcI4(500000)
						},
						matcher =>
						{
							return matcher
								.Insert(
									SequenceMatcherPastBoundsDirection.After, true,
									new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice)))
								);
						},
						minExpectedOccurences: 3,
						maxExpectedOccurences: 3
					)
					.ForEach(
						SequenceMatcherRelativeBounds.WholeSequence,
						new IElementMatch<CodeInstruction>[]
						{
							ILMatches.LdcI4(300000)
						},
						matcher =>
						{
							return matcher
								.Insert(
									SequenceMatcherPastBoundsDirection.After, true,
									new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(GetModifiedPrice)))
								);
						},
						minExpectedOccurences: 3,
						maxExpectedOccurences: 3
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Mod.Monitor.Log($"Could not patch methods - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		private static IEnumerable<CodeInstruction> JsonAssets_Mod_OnMenuChanged_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.Find(ILMatches.Instruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<ISalable, int[]>), nameof(Dictionary<ISalable, int[]>.Add), new Type[] { typeof(ISalable), typeof(int[]) })))
					.Replace(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(JsonAssetsOrDynamicGameAssets_Mod_OnMenuChanged_Transpiler_ModifyValues))))
					.AllElements();
			}
			catch (Exception ex)
			{
				Mod.Monitor.Log($"Could not patch methods - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		private static IEnumerable<CodeInstruction> DynamicGameAssets_ShopEntry_AddToShopOrAddToShopStock_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.ForEach(
						SequenceMatcherRelativeBounds.WholeSequence,
						new IElementMatch<CodeInstruction>[]
						{
							ILMatches.Instruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<ISalable, int[]>), nameof(Dictionary<ISalable, int[]>.Add), new Type[] { typeof(ISalable), typeof(int[]) }))
						},
						matcher =>
						{
							return matcher
								.Replace(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InflationAffix), nameof(JsonAssetsOrDynamicGameAssets_Mod_OnMenuChanged_Transpiler_ModifyValues))));
						},
						minExpectedOccurences: 3,
						maxExpectedOccurences: 3
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Mod.Monitor.Log($"Could not patch methods - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		public static void JsonAssetsOrDynamicGameAssets_Mod_OnMenuChanged_Transpiler_ModifyValues(Dictionary<ISalable, int[]> stock, ISalable item, int[] values)
		{
			if (Mod.ActiveAffixes.Any(a => a is InflationAffix) && values.Length == 2)
				ModifyPrice(ref values[0]);
			stock.Add(item, values);
		}
	}
}