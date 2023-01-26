namespace Shockah.SeasonAffixes
{
	internal interface IAffixProvider
	{
		ISeasonAffix? GetAffix(AffixScore? score = null);
	}
}