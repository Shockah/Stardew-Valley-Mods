using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class FortuneAffix : BaseSeasonAffix
	{
		private static string ShortID => "Fortune";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.mouseCursors, new(381, 361, 10, 10));

		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		public override int GetNegativity(OrdinalSeason season)
			=> 0;

		public override void OnActivate()
		{
			Mod.Helper.Events.GameLoop.DayStarted += OnDayStarted;

			if (!Context.IsMainPlayer)
				return;
			Game1.player.team.sharedDailyLuck.Value += Mod.Config.FortuneValue;
		}

		public override void OnDeactivate()
		{
			if (!Context.IsMainPlayer)
				return;
			Game1.player.team.sharedDailyLuck.Value -= Mod.Config.FortuneValue;
		}

		public override void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.positive.{ShortID}.config.value", () => Mod.Config.FortuneValue, min: 0.001f, max: 0.5f, interval: 0.001f, value => $"{(int)(value * 100):0.##}%");
		}

		private void OnDayStarted(object? sender, DayStartedEventArgs e)
		{
			if (!Context.IsMainPlayer)
				return;
			Game1.player.team.sharedDailyLuck.Value += Mod.Config.FortuneValue;
		}
	}
}