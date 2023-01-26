using Shockah.Kokoro.Stardew;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class DescentAffix : ISeasonAffix
	{
		private SeasonAffixes Mod { get; init; }

		private static string ShortID => "Descent";
		public string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public AffixScore Score => AffixScore.Positive;

		public DescentAffix(SeasonAffixes mod)
		{
			this.Mod = mod;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		double ISeasonAffix.GetProbabilityWeight(Season season, int year)
		{
			// TODO: return 0 if the Mine is finished, but the bus is not repaired (so Skull Cavern is not accessible)
			return 1;
		}

		// TODO: Descent implementation
	}
}