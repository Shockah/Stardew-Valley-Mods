using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class DroughtAffix : ISeasonAffix
	{
		private SeasonAffixes Mod { get; init; }

		private static string ShortID => "Drought";
		public string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.description");
		public TextureRectangle Icon => new(Game1.mouseCursors, new(413, 333, 13, 13));

		public DroughtAffix(SeasonAffixes mod)
		{
			this.Mod = mod;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetPositivity(Season season, int year)
			=> 0;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetNegativity(Season season, int year)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public double GetProbabilityWeight(Season season, int year)
			=> season == Season.Winter ? 0 : 1;

		bool ISeasonAffix.ShouldConflict(ISeasonAffix affix)
			=> affix.UniqueID == $"{Mod.ModManifest.UniqueID}.Thunder";

		// TODO: Drought implementation
	}
}