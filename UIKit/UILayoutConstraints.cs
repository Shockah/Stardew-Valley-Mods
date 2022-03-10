using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit
{
	public static class UILayoutConstraints
	{
		public static UIView? GetStoringView(this UILayoutConstraint self)
		{
			if (!self.IsActive)
				return null;
			return self.Anchor1.Owner.ConstrainableOwnerView.GetViewHierarchy(true).FirstOrDefault(v => v.Constraints.Contains(self))
				?? self.Anchor2?.Owner?.ConstrainableOwnerView?.GetViewHierarchy(true)?.FirstOrDefault(v => v.Constraints.Contains(self));
		}

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