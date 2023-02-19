using System;

namespace Shockah.Talented
{
	public record Talent(
		string UniqueID,
		Func<string> NameProvider,
		ITalent? ReplacedTalent,
		ITalentTag Tag,
		ITalentRequirements? Requirements,
		int PointCost = 1
	) : ITalent
	{
		public string Name
			=> NameProvider();
	}
}