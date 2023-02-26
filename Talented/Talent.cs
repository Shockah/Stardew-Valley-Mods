using Shockah.Kokoro.UI;
using System;

namespace Shockah.Talented
{
	public record Talent(
		string UniqueID,
		Func<TextureRectangle> IconProvider,
		Func<string> NameProvider,
		Func<string> DescriptionProvider,
		ITalent? ReplacedTalent,
		ITalentTag Tag,
		ITalentRequirements? Requirements,
		int PointCost = 1
	) : ITalent
	{
		public TextureRectangle Icon
			=> IconProvider();

		public string Name
			=> NameProvider();

		public string Description
			=> DescriptionProvider();
	}
}