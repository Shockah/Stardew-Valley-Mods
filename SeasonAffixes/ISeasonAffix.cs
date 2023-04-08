using Shockah.Kokoro.UI;

namespace Shockah.SeasonAffixes
{
	public interface ISeasonAffix
	{
		string UniqueID { get; }
		string LocalizedName { get; }
		string LocalizedDescription { get; }
		TextureRectangle Icon { get; }

		int GetPositivity(OrdinalSeason season);
		int GetNegativity(OrdinalSeason season);

		double GetProbabilityWeight(OrdinalSeason season)
			=> 1;
	}
}