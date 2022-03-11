using Cassowary;

namespace Shockah.UIKit
{
	internal static class CassowaryExt
	{
		public static bool RemoveConstraintIfExists(this ClSimplexSolver solver, ClConstraint constraint)
		{
			try
			{
				solver.RemoveConstraint(constraint);
				return true;
			}
			catch (CassowaryConstraintNotFoundException)
			{
				return false;
			}
		}
	}
}