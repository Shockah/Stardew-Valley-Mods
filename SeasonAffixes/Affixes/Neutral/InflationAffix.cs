using HarmonyLib;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewValley;
using System.Linq;
using System;
using StardewValley.Menus;
using System.Collections.Generic;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using StardewValley.Locations;
using System.Reflection.Emit;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Neutral
{
	internal sealed class InflationAffix : BaseSeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Inflation";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.description", new { Value = $"{(int)(Mod.Config.InflationIncrease * 100):0.##}%" });
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(272, 528, 16, 16));

		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		public override int GetNegativity(OrdinalSeason season)
			=> 1;

		public override void OnRegister()
			=> Apply(Mod.Harmony);

		public override void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.neutral.{ShortID}.config.increase", () => Mod.Config.InflationIncrease, min: 0.05f, max: 4f, interval: 0.05f, value => $"{(int)(value * 100):0.##}%");
		}

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatchVirtual(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(Item), nameof(Item.salePrice)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(InflationAffix), nameof(Item_salePrice_Postfix)))
			);
			harmony.TryPatchVirtual(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(SObject), nameof(SObject.sellToStorePrice)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(InflationAffix), nameof(SObject_sellToStorePrice_Postfix)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Constructor(typeof(ShopMenu), new Type[] { typeof(Dictionary<ISalable, int[]>), typeof(int), typeof(string), typeof(Func<ISalable, Farmer, int, bool>), typeof(Func<ISalable, bool>), typeof(string) }),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(InflationAffix), nameof(ShopMenu_ctor_Postfix)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Constructor(typeof(BluePrint), new Type[] { typeof(string) }),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(InflationAffix), nameof(BluePrint_ctor_Postfix)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(BusStop), nameof(BusStop.answerDialogue)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(InflationAffix), nameof(BusStop_answerDialogue_Transpiler)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(BoatTunnel), nameof(BoatTunnel.checkAction)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(InflationAffix), nameof(BoatTunnel_checkAction_Transpiler)))
			);
			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(BoatTunnel), nameof(BoatTunnel.answerDialogue)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(InflationAffix), nameof(BoatTunnel_answerDialogue_Transpiler)))
			);
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
				ModifyPrice(ref kvp.Value[0]);
		}

		private static void BluePrint_ctor_Postfix(BluePrint __instance)
		{
			if (!Mod.ActiveAffixes.Any(a => a is InflationAffix))
				return;
			ModifyPrice(ref __instance.GoldRequired);
		}

		private static IEnumerable<CodeInstruction> BusStop_answerDialogue_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.AsAnchorable<CodeInstruction, Guid, Guid, SequencePointerMatcher<CodeInstruction>, SequenceBlockMatcher<CodeInstruction>>()
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
					.AsAnchorable<CodeInstruction, Guid, Guid, SequencePointerMatcher<CodeInstruction>, SequenceBlockMatcher<CodeInstruction>>()
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
					.AsAnchorable<CodeInstruction, Guid, Guid, SequencePointerMatcher<CodeInstruction>, SequenceBlockMatcher<CodeInstruction>>()
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
	}
}