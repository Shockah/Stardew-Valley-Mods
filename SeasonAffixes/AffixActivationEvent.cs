namespace Shockah.SeasonAffixes;

public record AffixActivationEvent(
	ISeasonAffix Affix,
	AffixActivationContext Context
);