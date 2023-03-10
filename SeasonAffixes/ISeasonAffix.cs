using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;

namespace Shockah.SeasonAffixes
{
	public interface ISeasonAffix
	{
		string UniqueID { get; }
		string LocalizedName { get; }
		string LocalizedDescription { get; }
		TextureRectangle Icon { get; }

		int GetPositivity(Season season, int year);
		int GetNegativity(Season season, int year);

		double GetProbabilityWeight(Season season, int year)
			=> 1;

		protected internal bool ShouldConflict(ISeasonAffix affix)
			=> false;
	}

	public static class ISeasonAffixExt
	{
		public static bool Conflicts(this ISeasonAffix lhs, ISeasonAffix rhs)
			=> lhs.ShouldConflict(rhs) || rhs.ShouldConflict(lhs);
	}
}