using System.Collections.Generic;

namespace Shockah.UIKit
{
	public static class UILayoutConstraintExt
	{
		public static void Activate(this IEnumerable<UILayoutConstraint> constraints)
		{
			foreach (var constraint in constraints)
				constraint.Activate();
		}

		public static void Deactivate(this IEnumerable<UILayoutConstraint> constraints)
		{
			foreach (var constraint in constraints)
				constraint.Deactivate();
		}
	}
}