using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class FortuneAffix : BaseSeasonAffix
	{
		private static string ShortID => "Fortune";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.mouseCursors, new(381, 361, 10, 10));

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetNegativity(OrdinalSeason season)
			=> 0;

		public override void OnActivate()
		{
			Mod.Helper.Events.GameLoop.DayStarted += OnDayStarted;

			if (!Context.IsMainPlayer)
				return;
			Game1.player.team.sharedDailyLuck.Value += 0.05;
		}

		public override void OnDeactivate()
		{
			if (!Context.IsMainPlayer)
				return;
			Game1.player.team.sharedDailyLuck.Value -= 0.05;
		}

		private void OnDayStarted(object? sender, DayStartedEventArgs e)
		{
			if (!Context.IsMainPlayer)
				return;
			Game1.player.team.sharedDailyLuck.Value += 0.05;
		}
	}
}