using Shockah.Kokoro.UI;

namespace Shockah.Talented
{
	public interface ITalent
	{
		string UniqueID { get; }
		TextureRectangle Icon { get; }
		string Name { get; }
		string Description { get; }
		ITalent? ReplacedTalent { get; }
		ITalentTag Tag { get; }
		ITalentRequirements? Requirements { get; }
		int PointCost => 1;

		bool Matches(ITalentTag tag)
		{
			ITalentTag? current = Tag;
			while (true)
			{
				if (tag.Equals(current))
					return true;
				current = current.Parent;
				if (current == null)
					return false;
			}
		}
	}
}