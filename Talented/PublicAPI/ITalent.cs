namespace Shockah.Talented
{
	public interface ITalent
	{
		ITalentDefinition Definition { get; }
		int Rank { get; }
	}
}