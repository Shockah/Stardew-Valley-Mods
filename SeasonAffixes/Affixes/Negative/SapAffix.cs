using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class SapAffix : BaseSeasonAffix, ISeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Sap";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(320, 48, 16, 16));

		public override int GetPositivity(OrdinalSeason season)
			=> 0;

		public override int GetNegativity(OrdinalSeason season)
			=> 1;

		public override IReadOnlySet<string> Tags
			=> new HashSet<string> { VanillaSkill.TappingAspect };

		public override void OnRegister()
			=> Apply(Mod.Harmony);

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(Tree), nameof(Tree.UpdateTapperProduct)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(SapAffix), nameof(Tree_UpdateTapperProduct_Postfix)))
			);
		}

		private static void Tree_UpdateTapperProduct_Postfix(SObject tapper_instance)
		{
			if (!Mod.ActiveAffixes.Any(a => a is SapAffix))
				return;

			float timeMultiplier = tapper_instance.ParentSheetIndex == 264 ? 0.5f : 1f;
			Random random = new((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed + 73137);
			tapper_instance.heldObject.Value = new SObject(92, random.Next(3, 8));
			tapper_instance.MinutesUntilReady = Utility.CalculateMinutesUntilMorning(Game1.timeOfDay, (int)Math.Max(1.0, Math.Floor(1f * timeMultiplier)));
		}
	}
}