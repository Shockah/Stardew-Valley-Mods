using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Kokoro.UI;
using StardewValley;
using System.Linq;
using System;
using Shockah.CommonModCode.GMCM;
using StardewModdingAPI;
using Shockah.Kokoro.GMCM;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class RanchingAffix : BaseSeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Ranching";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description", new { Value = $"{(int)(Mod.Config.RanchingValue * 100):0.##}%" });
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(256, 272, 16, 16));

		public override int GetPositivity(OrdinalSeason season)
			=> Mod.Config.RanchingValue > 1f ? 1 : 0;

		public override int GetNegativity(OrdinalSeason season)
			=> Mod.Config.RanchingValue < 1f ? 1 : 0;

		public override void OnRegister()
			=> Apply(Mod.Harmony);

		public override void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.positive.{ShortID}.config.value", () => Mod.Config.RanchingValue, min: 0f, max: 4f, interval: 0.05f, value => $"{(int)(value * 100):0.##}%");
		}

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(SObject), nameof(SObject.sellToStorePrice)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(RanchingAffix), nameof(SObject_sellToStorePrice_Postfix)))
			);
		}

		private static void SObject_sellToStorePrice_Postfix(SObject __instance, ref int __result)
		{
			if (__result <= 0)
				return;
			if (!Mod.ActiveAffixes.Any(a => a is RanchingAffix))
				return;
			if (!(__instance.Category is SObject.EggCategory or SObject.MilkCategory or SObject.meatCategory or SObject.sellAtPierresAndMarnies))
				return;
			__result = (int)Math.Round(__result * Mod.Config.RanchingValue);
		}
	}
}