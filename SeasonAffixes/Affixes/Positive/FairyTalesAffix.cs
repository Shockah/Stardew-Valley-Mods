using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Shockah.Kokoro;
using Shockah.Kokoro.UI;
using StardewValley;
using StardewValley.Events;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class FairyTalesAffix : BaseSeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "FairyTales";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.content.Load<Texture2D>("LooseSprites\\temporary_sprites_1"), new(2, 129, 18, 16));

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
				original: () => AccessTools.Method(typeof(Utility), nameof(Utility.pickFarmEvent)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(FairyTalesAffix), nameof(Utility_pickFarmEvent_Postfix)))
			);
		}

		private static void Utility_pickFarmEvent_Postfix(ref FarmEvent? __result)
		{
			if (!Mod.ActiveAffixes.Any(a => a is FairyTalesAffix))
				return;
			if (__result is not null)
				return;

			Random random = new((int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame / 2);
			if (random.NextDouble() >= 0.15)
				return;
			__result = random.NextBool() ? new FairyEvent() : new WitchEvent();
		}
	}
}