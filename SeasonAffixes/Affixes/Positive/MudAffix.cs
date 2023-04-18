using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using StardewValley.Locations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class MudAffix : BaseSeasonAffix, ISeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Mud";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(288, 208, 16, 16));

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetNegativity(OrdinalSeason season)
			=> 0;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public double GetProbabilityWeight(OrdinalSeason season)
			=> season.Season == Season.Winter || Game1.whichFarm != 6 ? 0 : 1;

		public override void OnRegister()
			=> Apply(Mod.Harmony);

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(BuildableGameLocation), nameof(BuildableGameLocation.doesTileHaveProperty)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(MudAffix), nameof(BuildableGameLocation_doesTileHaveProperty_Postfix)))
			);

			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(BuildableGameLocation), nameof(BuildableGameLocation.doesTileHavePropertyNoNull)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(MudAffix), nameof(BuildableGameLocation_doesTileHavePropertyNoNull_Postfix)))
			);
		}

		private static void BuildableGameLocation_doesTileHaveProperty_Postfix(BuildableGameLocation __instance, string propertyName, ref string? __result)
		{
			if (!Mod.ActiveAffixes.Any(a => a is MudAffix))
				return;
			if (__instance is Farm && propertyName == "NoSprinklers")
				__result = null;
		}

		private static void BuildableGameLocation_doesTileHavePropertyNoNull_Postfix(BuildableGameLocation __instance, string propertyName, ref string __result)
		{
			if (!Mod.ActiveAffixes.Any(a => a is MudAffix))
				return;
			if (__instance is Farm && propertyName == "NoSprinklers")
				__result = "";
		}
	}
}