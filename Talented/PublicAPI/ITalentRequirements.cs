using System.Collections.Generic;

namespace Shockah.Talented
{
	public interface ITalentRequirements
	{
		bool AreSatisifed(IEnumerable<ITalent> talents);
	}
}