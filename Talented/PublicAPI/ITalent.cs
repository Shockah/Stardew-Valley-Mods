namespace Shockah.Talented
{
	public interface ITalent
	{
		string UniqueID { get; }
		string Name { get; set; }
		ITalent? ReplacedTalent { get; }
		ITalentTag Tag { get; }
		ITalentRequirements? Requirements { get; }
		int PointCost => 1;

		bool Matches(ITalentTag tag)
		{
			ITalentTag? current = Tag;
			while (true)
			{
				if (current == tag)
					return true;
				current = tag.Parent;
				if (current == null)
					return false;
			}
		}
	}
}