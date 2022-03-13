using Cassowary;
using System.Collections.Generic;

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
			catch (KeyNotFoundException)
			{
				return false;
			}
		}
	}
}