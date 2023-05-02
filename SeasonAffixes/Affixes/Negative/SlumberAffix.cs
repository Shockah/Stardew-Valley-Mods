using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class SlumberAffix : BaseSeasonAffix
	{
		private static string ShortID => "Slumber";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.description", new { Hours = $"{(int)(Mod.Config.SlumberHours):0.#}" });
		public override TextureRectangle Icon => new(Game1.emoteSpriteSheet, new(32, 96, 16, 16));

		public override int GetPositivity(OrdinalSeason season)
			=> 0;

		public override int GetNegativity(OrdinalSeason season)
			=> 1;

		public override void OnActivate()
		{
			Mod.Helper.Events.GameLoop.DayStarted += OnDayStarted;
		}

		public override void OnDeactivate()
		{
			Mod.Helper.Events.GameLoop.DayStarted -= OnDayStarted;
		}

		public override void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.negative.{ShortID}.config.hours", () => Mod.Config.SlumberHours, min: 0.5f, max: 12f, interval: 0.5f);
		}

		private void OnDayStarted(object? sender, DayStartedEventArgs e)
		{
			if (!Context.IsMainPlayer)
				return;

			int minutesToSkip = (int)Math.Round(Mod.Config.SlumberHours * 60) / 10 * 10;
			while (minutesToSkip > 0)
			{
				Game1.performTenMinuteClockUpdate();
				minutesToSkip -= 10;
			}
		}
	}
}