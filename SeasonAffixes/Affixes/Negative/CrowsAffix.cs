using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class CrowsAffix : BaseSeasonAffix, ISeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Crows";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.description");
		public TextureRectangle Icon => new(Game1.content.Load<Texture2D>(Critter.critterTexture), new(134, 46, 21, 17));

		public CrowsAffix() : base($"{Mod.ModManifest.UniqueID}.{ShortID}") { }

		public int GetPositivity(OrdinalSeason season)
			=> 0;

		public int GetNegativity(OrdinalSeason season)
			=> 1;

		public IReadOnlySet<string> Tags { get; init; } = new HashSet<string> { VanillaSkill.CropsAspect, VanillaSkill.FlowersAspect };

		public double GetProbabilityWeight(OrdinalSeason season)
			=> Mod.Config.WinterCrops || season.Season != Season.Winter ? 1 : 0;

		public void OnRegister()
			=> Apply(Mod.Harmony);

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(SObject), nameof(SObject.IsScarecrow)),
				postfix: new HarmonyMethod(AccessTools.Method(GetType(), nameof(SObject_IsScarecrow_Postfix)))
			);
		}

		private static void SObject_IsScarecrow_Postfix(ref bool __result)
		{
			if (!Mod.ActiveAffixes.Any(a => a is CrowsAffix))
				return;
			__result = false;
		}
	}
}