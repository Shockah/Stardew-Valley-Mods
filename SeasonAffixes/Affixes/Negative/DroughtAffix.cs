using Shockah.Kokoro.Stardew;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class DroughtAffix : ISeasonAffix
	{
		private SeasonAffixes Mod { get; init; }

		private static string ShortID => "Drought";
		public string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public AffixScore Score => AffixScore.Negative;

		public DroughtAffix(SeasonAffixes mod)
		{
			this.Mod = mod;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		double ISeasonAffix.GetProbabilityWeight(Season season, int year)
		{
			return season == Season.Winter ? 0 : 1;
		}

		// TODO: Drought implementation
	}
}