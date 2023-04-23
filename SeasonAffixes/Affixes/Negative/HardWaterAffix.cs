using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class HardWaterAffix : BaseSeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "HardWater";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(368, 384, 16, 16));

		public override int GetPositivity(OrdinalSeason season)
			=> 0;

		public override int GetNegativity(OrdinalSeason season)
			=> 1;

		public override IReadOnlySet<string> Tags
			=> new HashSet<string> { VanillaSkill.Farming.UniqueID };

		public override double GetProbabilityWeight(OrdinalSeason season)
		{
			bool greenhouseUnlocked = Game1.getAllFarmers().Any(p => p.mailReceived.Contains("ccVault") || p.mailReceived.Contains("jojaVault"));
			bool gingerIslandUnlocked = Game1.getAllFarmers().Any(p => p.mailReceived.Contains("willyBackRoomInvitation"));
			bool isWinter = season.Season == Season.Winter;
			return isWinter && !greenhouseUnlocked && !gingerIslandUnlocked ? 0 : 1;
		}

		public override void OnRegister()
			=> Apply(Mod.Harmony);

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(SObject), nameof(SObject.IsSprinkler)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(HardWaterAffix), nameof(SObject_IsSprinkler_Postfix)))
			);
		}

		private static void SObject_IsSprinkler_Postfix(ref bool __result)
		{
			if (!Mod.ActiveAffixes.Any(a => a is HardWaterAffix))
				return;
			__result = false;
		}
	}
}