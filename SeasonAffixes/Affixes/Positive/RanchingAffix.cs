using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Kokoro.UI;
using Shockah.SeasonAffixes.Affixes.Negative;
using StardewValley;
using System.Linq;
using System;
using System.Runtime.CompilerServices;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class RanchingAffix : BaseSeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Ranching";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(256, 272, 16, 16));

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetNegativity(OrdinalSeason season)
			=> 0;

		public override void OnRegister()
			=> Apply(Mod.Harmony);

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(SObject), nameof(SObject.sellToStorePrice)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(CrowsAffix), nameof(SObject_sellToStorePrice_Postfix)))
			);
		}

		private static void SObject_sellToStorePrice_Postfix(SObject __instance, ref int __result)
		{
			if (!Mod.ActiveAffixes.Any(a => a is RanchingAffix))
				return;
			if (__instance.Category is SObject.EggCategory or SObject.MilkCategory or SObject.meatCategory or SObject.sellAtPierresAndMarnies)
				__result = (int)Math.Ceiling(__result * 2.0);
		}
	}
}