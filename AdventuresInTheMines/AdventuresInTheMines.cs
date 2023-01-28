using HarmonyLib;
using Shockah.AdventuresInTheMines.Populators;
using Shockah.CommonModCode;
using Shockah.CommonModCode.Stardew;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.AdventuresInTheMines
{
	public class AdventuresInTheMines : Mod
	{
		private static AdventuresInTheMines Instance = null!;

		private List<IMineShaftPopulator> Populators { get; set; } = null!;

		public override void Entry(IModHelper helper)
		{
			Instance = this;

			helper.Events.GameLoop.DayStarted += OnDayStarted;

			var harmony = new Harmony(ModManifest.UniqueID);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(MineShaft), "populateLevel"),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(AdventuresInTheMines), nameof(MineShaft_populateLevel_Prefix))),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(AdventuresInTheMines), nameof(MineShaft_populateLevel_Postfix)))
			);
		}

		private void OnDayStarted(object? sender, DayStartedEventArgs e)
		{
			ResetPopulators();
		}

		private void ResetPopulators()
		{
			var lootProvider = new LimitedWithAlternativeLootProvider(
				main: new BirthdayPresentLootProvider(Game1.Date.GetByAddingDays(1)),
				alternative: new DefaultMineShaftLootProvider()
			);

			Populators = new()
			{
				new IcePuzzlePopulator(Monitor, lootProvider)
			};
		}

		private static void MineShaft_populateLevel_Prefix(MineShaft __instance)
		{
			foreach (var populator in Instance.Populators)
				populator.BeforePopulate(__instance);
		}

		private static void MineShaft_populateLevel_Postfix(MineShaft __instance)
		{
			foreach (var populator in ((IEnumerable<IMineShaftPopulator>)Instance.Populators).Reverse())
				populator.AfterPopulate(__instance);
		}
	}
}