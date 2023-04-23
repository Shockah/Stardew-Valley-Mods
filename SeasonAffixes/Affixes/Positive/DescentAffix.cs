using HarmonyLib;
using Microsoft.Xna.Framework;
using Shockah.Kokoro;
using Shockah.Kokoro.UI;
using StardewValley;
using StardewValley.Locations;
using System.Linq;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class DescentAffix : BaseSeasonAffix, ISeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Descent";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.bigCraftableSpriteSheet, new(112, 272, 16, 16));

		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		public override int GetNegativity(OrdinalSeason season)
			=> 0;

		public override double GetProbabilityWeight(OrdinalSeason season)
		{
			bool finishedMine = MineShaft.lowestLevelReached >= 120;
			bool busUnlocked = Game1.getAllFarmers().Any(p => p.mailReceived.Contains("ccVault") || p.mailReceived.Contains("jojaVault"));
			return busUnlocked || !finishedMine ? 1 : 0;
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
				original: () => AccessTools.Method(typeof(MineShaft), nameof(MineShaft.monsterDrop)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(DescentAffix), nameof(MineShaft_monsterDrop_Postfix)))
			);
		}

		private static void MineShaft_monsterDrop_Postfix(MineShaft __instance, int x, int y)
		{
			if (!Mod.ActiveAffixes.Any(a => a is DescentAffix))
				return;
			if (__instance.mustKillAllMonstersToAdvance())
				return;
			if (__instance.EnemyCount > 1)
				return;
			__instance.recursiveTryToCreateLadderDown(new Vector2((int)(x / 64f), (int)(y / 64f)), "newArtifact", 200);
		}
	}
}