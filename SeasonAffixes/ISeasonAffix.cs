using Shockah.Kokoro.UI;
using StardewModdingAPI;
using System.Collections.Generic;

#nullable enable

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

		void SetupConfig(IManifest manifest) { }
		void OnSaveConfig() { }

		int GetPositivity(OrdinalSeason season);
		int GetNegativity(OrdinalSeason season);

		IReadOnlySet<string> Tags
			=> new HashSet<string>();

		double GetProbabilityWeight(OrdinalSeason season)
			=> 1;
	}

	internal abstract class BaseSeasonAffix : ISeasonAffix
	{
		protected static SeasonAffixes Mod
			=> SeasonAffixes.Instance;

		public abstract string UniqueID { get; }
		public abstract string LocalizedName { get; }
		public abstract string LocalizedDescription { get; }
		public abstract TextureRectangle Icon { get; }

		public virtual void OnRegister() { }
		public virtual void OnUnregister() { }
		public virtual void OnActivate() { }
		public virtual void OnDeactivate() { }

		public virtual void SetupConfig(IManifest manifest) { }
		public virtual void OnSaveConfig() { }

		public abstract int GetNegativity(OrdinalSeason season);
		public abstract int GetPositivity(OrdinalSeason season);

        public virtual IReadOnlySet<string> Tags
			=> new HashSet<string>();

		public virtual double GetProbabilityWeight(OrdinalSeason season)
			=> 1;

		public override bool Equals(object? obj)
			=> obj is ISeasonAffix affix && UniqueID == affix.UniqueID;

		public override int GetHashCode()
			=> UniqueID.GetHashCode();
	}
}