using System.Collections.Generic;

namespace Shockah.Talented
{
	public interface ITalentDefinition
	{
		string UniqueID { get; }
		IReadOnlySet<string> Tags { get; }
		int MaxRank => 1;

		string GetName(int rank);
	}
}