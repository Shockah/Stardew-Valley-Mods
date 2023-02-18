using System.Collections.Generic;

namespace Shockah.Talented
{
	public interface ITalentDefinition
	{
		string UniqueID { get; }
		IReadOnlySet<ITalentTag> Tags { get; }
		int MaxRank => 1;

		string GetName(int rank);
	}
}