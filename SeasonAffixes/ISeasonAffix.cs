using Shockah.Kokoro.UI;
using StardewModdingAPI;
using System.Collections.Generic;

#nullable enable

namespace Shockah.SeasonAffixes;

public interface ISeasonAffix
{
	string UniqueID { get; }
	string LocalizedName { get; }
	string LocalizedDescription { get; }
	TextureRectangle Icon { get; }

	void OnRegister() { }
	void OnUnregister() { }

	void OnActivate(AffixActivationContext context) { }
	void OnDeactivate(AffixActivationContext context) { }

	void SetupConfig(IManifest manifest) { }
	void OnSaveConfig() { }

	int GetPositivity(OrdinalSeason season);
	int GetNegativity(OrdinalSeason season);

	IReadOnlySet<string> Tags
		=> new HashSet<string>();

	double GetProbabilityWeight(OrdinalSeason season)
		=> 1;
}