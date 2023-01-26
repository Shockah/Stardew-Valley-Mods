using Shockah.Kokoro.Stardew;

namespace Shockah.SeasonAffixes
{
	public interface ISeasonAffix
	{
		string UniqueID { get; }
		string LocalizedName { get; }
		string LocalizedDescription { get; }

		AffixScore Score { get; }

		double GetProbabilityWeight(Season season, int year)
			=> 1;
	}
}