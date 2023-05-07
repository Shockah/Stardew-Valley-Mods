using HarmonyLib;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class AgricultureAffix : BaseSeasonAffix, ISeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Agriculture";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description", new { Value = $"{(int)(Mod.Config.AgricultureValue * 100):0.##}%" });
		public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(96, 176, 16, 16));

		public AgricultureAffix() : base($"{Mod.ModManifest.UniqueID}.{ShortID}") { }

		public int GetPositivity(OrdinalSeason season)
			=> Mod.Config.AgricultureValue > 1f ? 1 : 0;

		public int GetNegativity(OrdinalSeason season)
			=> Mod.Config.AgricultureValue < 1f ? 1 : 0;

		public IReadOnlySet<string> Tags { get; init; } = new HashSet<string> { VanillaSkill.CropsAspect };

		public void OnRegister()
			=> Apply(Mod.Harmony);

		public void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.positive.{ShortID}.config.value", () => Mod.Config.AgricultureValue, min: 0f, max: 4f, interval: 0.05f, value => $"{(int)(value * 100):0.##}%");
		}

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(SObject), nameof(SObject.sellToStorePrice)),
				postfix: new HarmonyMethod(AccessTools.Method(GetType(), nameof(SObject_sellToStorePrice_Postfix)))
			);
		}

		private static void SObject_sellToStorePrice_Postfix(SObject __instance, ref int __result)
		{
			if (__result <= 0)
				return;
			if (!Mod.ActiveAffixes.Any(a => a is AgricultureAffix))
				return;
			if (!(__instance.Category is SObject.FruitsCategory or SObject.VegetableCategory))
				return;
			__result = (int)Math.Round(__result * Mod.Config.AgricultureValue);
		}
	}
}