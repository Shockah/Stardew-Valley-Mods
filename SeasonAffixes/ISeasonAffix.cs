using Shockah.Kokoro.UI;

namespace Shockah.SeasonAffixes
{
	public interface ISeasonAffix
	{
		string UniqueID { get; }
		string LocalizedName { get; }
		string LocalizedDescription { get; }
		TextureRectangle Icon { get; }

		void OnRegister() { }
		void OnUnregister() { }
		void OnActivate() { }
		void OnDeactivate() { }

		int GetPositivity(OrdinalSeason season);
		int GetNegativity(OrdinalSeason season);

		double GetProbabilityWeight(OrdinalSeason season)
			=> 1;
	}

	internal abstract class BaseSeasonAffix : ISeasonAffix
	{
		public abstract string UniqueID { get; }
		public abstract string LocalizedName { get; }
		public abstract string LocalizedDescription { get; }
		public abstract TextureRectangle Icon { get; }

		public virtual void OnRegister() { }
		public virtual void OnUnregister() { }
		public virtual void OnActivate() { }
		public virtual void OnDeactivate() { }

		public abstract int GetNegativity(OrdinalSeason season);
		public abstract int GetPositivity(OrdinalSeason season);

		public override bool Equals(object? obj)
			=> obj is ISeasonAffix affix && UniqueID == affix.UniqueID;

		public override int GetHashCode()
			=> UniqueID.GetHashCode();
	}
}